import tensorflow as tf
import tf2onnx

# Tải mô hình từ tệp .keras
model = tf.keras.models.load_model('lottery_lstm_model.keras')

# Chuyển đổi mô hình TensorFlow Keras sang ONNX sử dụng tf.function
# Tạo một hàm tf.function để chuyển đổi
@tf.function(input_signature=[tf.TensorSpec([None, 20, 5], tf.float32)])  # Định nghĩa shape của input
def model_fn(x):
    return model(x)

# Chuyển đổi mô hình từ tf.function sang ONNX
onnx_model = tf2onnx.convert.from_function(model_fn)

# Lưu mô hình ONNX ra tệp
onnx_model.save("lottery_lstm_model.onnx")
