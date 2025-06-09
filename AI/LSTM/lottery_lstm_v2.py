import numpy as np
import pandas as pd
import requests
import tensorflow as tf
from tensorflow.keras.models import Sequential
from tensorflow.keras.layers import LSTM, Dense

# === 1. Kết nối và tải dữ liệu từ API ===
API_URL = "https://api.insight.ai.vn/api/LuckyNumber/historical-prizetype-flat"  # Thay thế URL này bằng URL API của bạn

params = {
    "fromDate": '20-01-2004'
}

response = requests.get(API_URL, params=params)
data = (response.json())['data']

# Chuyển dữ liệu thành DataFrame
df = pd.DataFrame(data)

# === 2. Sắp xếp dữ liệu theo thời gian ===
df['date'] = pd.to_datetime(df['date'])
df = df.sort_values(by='date')

# === 3. Tiền xử lý dữ liệu ===
PAD = 10  # Giá trị padding
VOCAB_SIZE = 10  # Các số từ 0 đến 9

# Mã hóa chuỗi số trúng
def encode_number(num, max_len):
    arr = [int(ch) for ch in num]
    while len(arr) < max_len:
        arr.append(PAD)
    return arr[:max_len]

# Tìm độ dài tối đa của chuỗi số trúng
max_len = max(len(num) for sublist in df['numbers'] for num in sublist)

# Mã hóa các chuỗi số trúng
encoded_numbers = []
for numbers in df['numbers']:
    encoded_numbers.append([encode_number(num, max_len) for num in numbers])

df['encoded_numbers'] = encoded_numbers

# === 4. Tạo dữ liệu huấn luyện ===
WINDOW_SIZE = 2
X, Y = [], []

# Xử lý dữ liệu và tạo mẫu cho mô hình
for i in range(len(encoded_numbers) - WINDOW_SIZE):
    # Tạo mảng numpy rỗng cho đầu vào X và đầu ra Y
    temp_input = np.zeros((WINDOW_SIZE, max_len), dtype=np.float32)  # Mảng rỗng với kích thước (WINDOW_SIZE, max_len)
    temp_output = np.zeros((WINDOW_SIZE, max_len), dtype=np.float32)  # Mảng rỗng cho output
    
    # Lấy WINDOW_SIZE chuỗi trúng số cho đầu vào (X)
    for j in range(WINDOW_SIZE):
        temp_input[j, :] = np.array(encoded_numbers[i + j][0])  # Chọn chuỗi đầu tiên trong danh sách (hoặc điều chỉnh tùy theo logic của bạn)
    
    # Lấy chuỗi tiếp theo cho đầu ra (Y)
    for k in range(WINDOW_SIZE):
        # Lấy chuỗi đầu tiên trong danh sách số trúng (nếu có nhiều chuỗi)
        temp_output[k, :] = np.array(encoded_numbers[i + k + 1][0])  # Chọn chuỗi đầu tiên trong danh sách

    X.append(temp_input)
    Y.append(temp_output)

# Chuyển X và Y thành mảng NumPy với hình dạng đồng nhất
X = np.array(X, dtype=np.float32)  # (samples, WINDOW_SIZE, max_len)
Y = np.array(Y, dtype=np.float32)  # (samples, WINDOW_SIZE, max_len)

# Kiểm tra lại độ đồng nhất của dữ liệu đầu vào
print("X shape:", X.shape)
print("Y shape:", Y.shape)

# One-hot encoding Y
Y_onehot = np.zeros((Y.shape[0], Y.shape[1], VOCAB_SIZE), dtype=np.float32)
for i in range(Y.shape[0]):
    for j in range(Y.shape[1]):
        # Lấy giá trị số duy nhất từ chuỗi để thực hiện one-hot encoding
        Y_onehot[i, j, int(Y[i, j][0])] = 1.0  # Lấy giá trị số duy nhất từ chuỗi Y[i, j]

# === 5. Xây dựng mô hình LSTM ===
model = Sequential()
model.add(LSTM(256, input_shape=(X.shape[1], X.shape[2]), return_sequences=True))
model.add(LSTM(128, return_sequences=True))
model.add(Dense(VOCAB_SIZE, activation='softmax'))
model.compile(optimizer='adam', loss='categorical_crossentropy')

# Reshape X để phù hợp với LSTM input
X = X.reshape(X.shape[0], X.shape[1], X.shape[2])

# Huấn luyện mô hình
model.fit(X, Y_onehot, epochs=20, batch_size=64)

model.save("lottery_lstm_model_v2.keras")
print("Đã lưu mô hình: lottery_lstm_model_v2.keras")

model.save("lottery_lstm_model_v2.h5")
print("Đã lưu mô hình: lottery_lstm_model_v2.h5")

# === 6. Đề xuất new_sequence cho dự đoán ===
def get_last_sequence(df, prize_type):
    # Lọc dữ liệu theo loại giải
    filtered_data = df[df['kind'] == prize_type]
    # Lấy chuỗi số trúng của ngày gần nhất (chuỗi cuối cùng trong danh sách)
    last_sequence = filtered_data['encoded_numbers'].iloc[-1][0]  # Lấy chuỗi số trúng của giải cuối cùng
    return last_sequence

# Lọc theo giải người dùng chọn
prize_type = input("Nhập loại giải (ĐB, G1, G2, ..., G7): ")
N = int(input("Nhập số chuỗi trúng cần dự đoán (N): "))

# Đề xuất new_sequence cho dự đoán
new_sequence = get_last_sequence(df, prize_type)
print(f"Đề xuất new_sequence cho giải {prize_type}: {new_sequence}")

# Mã hóa và chuẩn bị đầu vào cho dự đoán
new_sequence_encoded = encode_number(new_sequence, max_len)

# Reshape đầu vào cho mô hình LSTM
new_sequence_input = np.array(new_sequence_encoded).reshape(1, 1, max_len)  # Reshape to (1, 1, max_len)

# Dự đoán N chuỗi trúng
def predict_multiple_sequences(model, input_sequence, N=5):
    predicted_sequences = []
    for _ in range(N):
        predicted_sequence = model.predict(input_sequence)
        predicted_values = np.argmax(predicted_sequence, axis=-1)
        predicted_sequences.append(predicted_values[0])
        
        # Cập nhật đầu vào cho lần dự đoán tiếp theo (dùng chuỗi dự đoán mới làm đầu vào)
        input_sequence = np.array(predicted_values).reshape(1, 1, max_len)  # Reshape cho lần dự đoán tiếp theo
        
    return predicted_sequences

# Dự đoán N chuỗi trúng
predicted_sequences = predict_multiple_sequences(model, new_sequence_input, N)

# In ra các chuỗi dự đoán
print(f"Predicted {N} sequences for prize type {prize_type}:")
for seq in predicted_sequences:
    print(seq)
