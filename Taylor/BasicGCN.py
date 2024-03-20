import tensorflow as tf
import Preprocess
from tensorflow import keras
from tensorflow.keras import layers
from spektral.layers import GCNConv

def create_gcn_model(input_shape):
    # Define the GCN model architecture
    inputs = keras.Input(shape=input_shape)
    x = GCNConv(32, activation="relu")(inputs)
    x = GCNConv(64, activation="relu")(x)
    x = layers.GlobalAveragePooling2D()(x)
    x = layers.Dense(128, activation="relu")(x)
    outputs = layers.Dense(1, activation="tanh")(x)

    model = keras.Model(inputs=inputs, outputs=outputs)
    return model

def train_model(model, data, labels, epochs, batch_size):
    # Compile the model
    model.compile(optimizer="adam", loss="mse", metrics=["mae"])

    # Train the model
    model.fit(data, labels, epochs=epochs, batch_size=batch_size)

def evaluate_model(model, data, labels):
    # Evaluate the model
    loss, mae = model.evaluate(data, labels)
    print(f"Test Loss: {loss:.4f}")
    print(f"Test MAE: {mae:.4f}")

def load_gcn_tensors():
    # Load the data from the file
    data, labels = Preprocess.load_gcn_matrices()

    # print(data)
    # print(labels)

    # Convert data and labels to TensorFlow tensors
    data = tf.convert_to_tensor(data, dtype=tf.float32)
    labels = tf.convert_to_tensor(labels, dtype=tf.float32)

    return data, labels

# Set the input shape based on the tensor dimensions
input_shape = (6, 6, 7)

if __name__ == "__main__":
    # Create the GCN model
    # model = create_gcn_model(input_shape)

    # Load the data from a file
    data, labels = load_gcn_tensors()

    # # Train the model
    # epochs = 10
    # batch_size = 32
    # train_model(model, data, labels, epochs, batch_size)

    # # Evaluate the model
    # evaluate_model(model, data, labels)
