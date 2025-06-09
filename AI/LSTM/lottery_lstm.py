import requests
import numpy as np
from tensorflow.keras.models import Sequential
from tensorflow.keras.layers import LSTM, Dense, TimeDistributed
from tensorflow.keras.utils import to_categorical

# ==== Config biến nằm trên cùng ====

API_URL = "https://api.insight.ai.vn/api/LuckyNumber/historical-sequences?prizeType=%C4%90B&yearsBack=1"  # Đổi thành URL thực tế
PRIZE_TYPE = "ĐB"            # Loại giải muốn train
YEARS_BACK = 15              # Lấy dữ liệu 15 năm gần nhất
WINDOW_SIZE = 20             # Số ngày liên tiếp làm input
PAD = 10                     # Giá trị padding (ngoài 0-9)
VOCAB_SIZE = 11              # 0-9 + padding
EPOCHS = 120                 # Số epoch train
BATCH_SIZE = 64              # Kích thước batch

# ==== Bước 1: Lấy dữ liệu lịch sử từ API .NET ====

params = {
    "prizeType": PRIZE_TYPE,
    "yearsBack": YEARS_BACK
}

response = requests.get(API_URL, params=params)
response.raise_for_status()
data = (response.json())['data']  # Danh sách dict {'Date': 'dd-MM-yyyy', 'Number': 'chuỗi số'}

numbers = [item['number'] for item in data]
print(f"Lấy được {len(numbers)} chuỗi số cho giải {PRIZE_TYPE}")

# ==== Bước 2: Tiền xử lý - mã hóa chuỗi số và padding ====

max_len = max(len(num) for num in numbers)
print(f"Độ dài chuỗi số tối đa: {max_len}")

def encode_number(num, max_len):
    arr = [int(ch) for ch in num]
    while len(arr) < max_len:
        arr.append(PAD)
    return arr[:max_len]

encoded_numbers = [encode_number(num, max_len) for num in numbers]

# ==== Bước 3: Tạo tập dữ liệu train dạng sequences (X) → tiếp theo (Y) ====

X, Y = [], []
for i in range(len(encoded_numbers) - WINDOW_SIZE):
    X.append(encoded_numbers[i:i+WINDOW_SIZE])     # WINDOW_SIZE chuỗi làm input
    Y.append(encoded_numbers[i+1:i+1+WINDOW_SIZE])  # Y được lấy từ chuỗi tiếp theo, có chiều dài WINDOW_SIZE

X = np.array(X)  # (samples, WINDOW_SIZE, max_len)
Y = np.array(Y)  # (samples, WINDOW_SIZE, max_len)

print(f"Số mẫu train: {X.shape[0]}, mỗi mẫu input shape: {X.shape[1:]}")

# ==== Bước 4: Chuẩn hóa dữ liệu cho mô hình LSTM ====

# Mô hình dự đoán mỗi ký tự, nên output Y phải one-hot theo VOCAB_SIZE
Y_onehot = np.zeros((Y.shape[0], Y.shape[1], VOCAB_SIZE), dtype=np.float32)
for i in range(Y.shape[0]):
    for j in range(Y.shape[1]):
        Y_onehot[i, j, Y[i, j]] = 1.0

print(f"Y_onehot shape: {Y_onehot.shape}")

# ==== Bước 5: Xây dựng mô hình LSTM deep ====

model = Sequential()
model.add(LSTM(256, input_shape=(WINDOW_SIZE, max_len), return_sequences=True))  # input_shape thay đổi thành (WINDOW_SIZE, max_len)
model.add(LSTM(128, return_sequences=True))
model.add(TimeDistributed(Dense(VOCAB_SIZE, activation='softmax')))

model.compile(optimizer='adam', loss='categorical_crossentropy')

model.summary()

# ==== Bước 6: Train mô hình ====

model.fit(X, Y_onehot, epochs=EPOCHS, batch_size=BATCH_SIZE, validation_split=0.1)

# ==== Bước 7: Lưu model ====

model.save("lottery_lstm_model.keras")
print("Đã lưu model: lottery_lstm_model.keras")
