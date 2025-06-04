import numpy as np
import pandas as pd
import tensorflow as tf
import requests

# === 1. Tải mô hình đã huấn luyện ===
model = tf.keras.models.load_model('lottery_lstm_model_v2.h5')  # Tải mô hình đã huấn luyện
print("Đã tải mô hình: lottery_lstm_model_v2.h5")

# === 2. Kết nối và tải dữ liệu từ API ===
API_URL = "https://localhost:60632/api/LuckyNumber/historical-prizetype-flat"  # Thay thế URL này bằng URL API của bạn

params = {
    "fromDate": '20-01-2004'
}

response = requests.get(API_URL, params=params, verify=False)
data = (response.json())['data']

# Chuyển dữ liệu thành DataFrame
df = pd.DataFrame(data)

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

# === 4. Đề xuất new_sequence cho dự đoán ===
def get_last_sequence(df, prize_type):
    # Lọc dữ liệu theo loại giải
    filtered_data = df[df['kind'] == prize_type]
    # Lấy chuỗi số trúng của ngày gần nhất (chuỗi cuối cùng trong danh sách)
    last_sequence = filtered_data['encoded_numbers'].iloc[-1][0]  # Lấy chuỗi số trúng của giải cuối cùng
    return last_sequence

# === 5. Dự đoán nhiều chuỗi (N chuỗi) ===
def predict_multiple_sequences(model, input_sequence, N=5):
    predicted_sequences = []
    
    # Mỗi lần dự đoán sẽ dùng kết quả trước đó làm đầu vào
    for _ in range(N):
        # Dự đoán chuỗi tiếp theo
        predicted_sequence = model.predict(input_sequence)
        
        print(f"predicted_sequence: {predicted_sequence}")  # Kiểm tra giá trị đầu ra
        
        # Chọn giá trị dự đoán có xác suất cao nhất từ mô hình
        predicted_values = np.argmax(predicted_sequence, axis=-1)  # Lấy chỉ số của giá trị dự đoán cao nhất
        
        print(f"predicted_values: {predicted_values}")  # Kiểm tra giá trị predicted_values
        
        # Giới hạn dự đoán trong phạm vi [0, 9] và đảm bảo không có giá trị ngoài phạm vi
        predicted_values = np.clip(predicted_values, 0, VOCAB_SIZE - 1)
        
        # Tạo chuỗi dự đoán với độ dài max_len
        predicted_sequence_values = [int(val) for val in predicted_values[0]]  # Chuyển thành giá trị số nguyên
        
        # Đảm bảo độ dài chuỗi bằng max_len
        predicted_sequence_values = predicted_sequence_values[:max_len]
        
        print(f"predicted_sequence_values: {predicted_sequence_values}")
        
        predicted_sequence_values.extend([PAD] * (max_len - len(predicted_sequence_values)))  # Padding nếu thiếu

        # Lưu chuỗi dự đoán vào danh sách
        predicted_sequences.append(predicted_sequence_values)
        
        # Cập nhật đầu vào cho lần dự đoán tiếp theo (dùng chuỗi dự đoán mới làm đầu vào)
        # Chú ý: input_sequence cần được reshaped lại từ predicted_sequence_values
        input_sequence = np.array(predicted_sequence_values).reshape(1, 1, max_len)  # Cập nhật đầu vào cho lần dự đoán tiếp theo
        
    return predicted_sequences

# === 6. Hỏi người dùng và thực hiện dự đoán ===
prize_type = input("Nhập loại giải (ĐB, G1, G2, ..., G7): ")  # Loại giải (ĐB, G1, G2, ...)
N = int(input("Nhập số chuỗi trúng cần dự đoán (N): "))  # Số chuỗi trúng cần dự đoán

# Đề xuất chuỗi mới (new_sequence) từ giải người dùng chọn
new_sequence = get_last_sequence(df, prize_type)
print(f"Đề xuất new_sequence cho giải {prize_type}: {new_sequence}")

# Mã hóa và chuẩn bị đầu vào cho dự đoán
new_sequence_encoded = encode_number(new_sequence, max_len)

# Reshape đầu vào cho mô hình LSTM
new_sequence_input = np.array(new_sequence_encoded).reshape(1, 1, max_len)  # Reshape to (1, 1, max_len)

# Dự đoán N chuỗi trúng
predicted_sequences = predict_multiple_sequences(model, new_sequence_input, N)

# In ra các chuỗi dự đoán
print(f"Predicted {N} sequences for prize type {prize_type}:")
for seq in predicted_sequences:
    print(seq)
