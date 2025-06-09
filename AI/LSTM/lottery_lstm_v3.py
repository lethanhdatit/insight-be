import numpy as np
import pandas as pd
import requests
from tensorflow.keras.models import Sequential
from tensorflow.keras.layers import LSTM, Dense
from sklearn.preprocessing import MinMaxScaler
import joblib

# Lấy dữ liệu từ API
API_URL = "https://api.insight.ai.vn/api/LuckyNumber/historical-prizetype-flat"

def get_lottery_data():
    params = {
        "fromDate": '20-01-2000'
    }
    response = requests.get(API_URL, params=params)
    data = (response.json())['data']
    return data

# Tiền xử lý dữ liệu
def preprocess_data(data):
    records = []
    for entry in data:
        kind = entry['kind']
        numbers = entry['numbers']
        for number in numbers:
            digits = list(number)
            digits += ['0'] * (10 - len(digits))  # Điền thêm số '0' nếu thiếu
            records.append([kind] + digits)  # Thêm loại giải vào đầu chuỗi

    df = pd.DataFrame(records, columns=['kind'] + [f'digit_{i}' for i in range(1, 11)])
    return df

# Tiền xử lý dữ liệu cho LSTM
def preprocess_lstm_data(df):
    kind_data = df.dropna()  # Bỏ các hàng có giá trị NaN
    kind_data = kind_data.drop(columns=[col for col in kind_data.columns if 'kind' in col])  # Bỏ cột One-Hot Encoding

    # Chuẩn hóa dữ liệu
    scaler = MinMaxScaler(feature_range=(0, 1))
    scaled_data = scaler.fit_transform(kind_data)

    X, y = [], []
    for i in range(len(scaled_data) - 1):
        X.append(scaled_data[i])  # Các chuỗi số đã biết
        y.append(scaled_data[i + 1])  # Chuỗi số tiếp theo
    X = np.array(X)
    y = np.array(y)

    # Reshape cho LSTM
    X = X.reshape(X.shape[0], X.shape[1], 1)
    return X, y, scaler

# Xây dựng mô hình LSTM
def build_lstm_model(input_shape):
    model = Sequential()
    model.add(LSTM(units=50, return_sequences=False, input_shape=input_shape))
    model.add(Dense(units=10))  # Output là 10 số
    model.compile(optimizer='adam', loss='mean_squared_error')
    return model

# Huấn luyện mô hình LSTM cho từng loại giải và lưu mô hình đã huấn luyện và scaler
def train_and_save_model_for_all_kinds():
    data = get_lottery_data()
    df = preprocess_data(data)

    # kinds = ['ĐB', 'G1', 'G2', 'G3', 'G4', 'G5', 'G6', 'G7']  # Các loại giải cần huấn luyện riêng biệt
    kinds = ['G3']  # Các loại giải cần huấn luyện riêng biệt

    for kind in kinds:
        kind_data = df[df['kind'] == kind]  # Lọc dữ liệu cho loại giải này
        X, y, scaler = preprocess_lstm_data(kind_data)

        # Xây dựng và huấn luyện mô hình
        model = build_lstm_model(input_shape=(X.shape[1], 1))
        model.fit(X, y, epochs=200, batch_size=5, verbose=2)

        # Lưu mô hình vào tệp
        model.save(f"lottery_model_{kind}.h5")  # Lưu mô hình cho từng loại giải

        # Lưu scaler vào tệp
        joblib.dump(scaler, f'scaler_{kind}.pkl')  # Lưu scaler cho từng loại giải

        print(f"Model và scaler cho giải {kind} đã được lưu.")

train_and_save_model_for_all_kinds()
