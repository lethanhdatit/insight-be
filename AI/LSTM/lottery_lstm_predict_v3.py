from tensorflow.keras.models import load_model
import numpy as np
import pandas as pd
import requests
from sklearn.preprocessing import MinMaxScaler
import joblib

# Lấy dữ liệu từ API
API_URL = "https://api.insight.ai.vn/api/LuckyNumber/historical-prizetype-flat"

def get_lottery_data():
    params = {
        "fromDate": '20-01-2004'
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

# Dự đoán kết quả dựa trên lựa chọn người dùng
def predict_results(model, scaler, kind, num_results):
    data = get_lottery_data()
    df = preprocess_data(data)
    
    # Lọc các chuỗi trúng giải gần đây nhất cho loại giải đã chọn
    kind_data = [entry for entry in data if entry['kind'] == kind]  # Lọc các kết quả theo 'kind'
    
    # Nếu không có kết quả cho loại giải đã chọn, trả về thông báo lỗi
    if not kind_data:
        print(f"Không có kết quả cho loại giải {kind}.")
        return []
    
    # Lấy kết quả trúng giải gần đây nhất cho loại giải đã chọn
    last_result = kind_data[-1]['numbers']  # Lấy kết quả trúng gần đây nhất từ dữ liệu
    num_digits = len(last_result[0])  # Số lượng chữ số của chuỗi kết quả đầu tiên

    scaled_data = scaler.transform(df.drop(columns=[col for col in df.columns if 'kind' in col]))  # Loại bỏ cột 'kind' One-Hot

    # Dự đoán chuỗi số tiếp theo
    last_sequence = scaled_data[-1]  # Lấy chuỗi số cuối cùng đã biết
    predictions = []

    for _ in range(num_results):
        # Dự đoán chuỗi số tiếp theo từ chuỗi cuối cùng
        next_number = model.predict(last_sequence.reshape(1, last_sequence.shape[0], 1))  # Dự đoán
        predicted_number = scaler.inverse_transform(next_number)  # Chuyển về giá trị ban đầu
        
        # Làm tròn kết quả và chuyển thành số nguyên
        predicted_number_int = np.round(predicted_number.flatten()).astype(int)
        
        # Giới hạn giá trị dự đoán để đảm bảo không bị âm hoặc vượt quá phạm vi
        predicted_number_int = np.clip(predicted_number_int, 0, 9)
        
        # Chuyển đổi predicted_number_int thành một chuỗi có đúng số lượng chữ số
        predicted_number_str = ''.join(map(str, predicted_number_int))[:num_digits]

        predictions.append(predicted_number_str)  # Lưu dự đoán dưới dạng chuỗi có đúng số chữ số

        # Cập nhật chuỗi số cuối cùng cho lần dự đoán tiếp theo
        last_sequence = np.roll(last_sequence, -1, axis=0)  # Dịch chuyển chuỗi để dự đoán tiếp
        last_sequence[-1] = predicted_number_int[0]  # Chỉ lấy giá trị đầu tiên của dự đoán và cập nhật vào last_sequence

    return predictions

# Tải mô hình và scaler đã lưu
def load_model_and_scaler(kind):
    try:
        model = load_model(f"lottery_model_{kind}.h5")  # Tải mô hình cho loại giải đã chọn
        scaler = joblib.load(f'scaler_{kind}.pkl')  # Tải scaler cho loại giải đã chọn
        print(f"Mô hình và scaler cho giải {kind} đã được tải thành công.")
        return model, scaler
    except:
        print(f"Không tìm thấy mô hình hoặc scaler cho giải {kind}, huấn luyện lại...")
        return None, None

# Sử dụng mô hình đã huấn luyện và scaler đã tải để dự đoán
kind = "G7"  # Ví dụ, người dùng chọn giải G7
model, scaler = load_model_and_scaler(kind)  # Tải mô hình và scaler cho loại giải đã chọn
if model and scaler:
    predicted_numbers = predict_results(model, scaler, kind, 10)

    for i in predicted_numbers:
        print("predicted_numbers:", i)
