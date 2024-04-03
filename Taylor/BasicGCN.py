import tensorflow as tf
import Preprocess
from tensorflow import keras
from tensorflow.keras import layers
from tensorflow.keras.models import save_model
from spektral.layers import GCNConv
import pandas as pd
import numpy as np
import random

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
    model.compile(optimizer="adam", loss="mse", metrics=["accuracy"])

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
    if data is None or labels is None:
        return None, None

    # Convert data and labels to numpy arrays
    data = np.array(data, dtype=np.int16)
    labels = np.array(labels, dtype=np.int8)

    # Reshape data to have the desired format: (num_samples, num_nodes, num_nodes, num_features)
    num_samples = data.shape[0]
    num_features = data.shape[1]
    num_nodes = data.shape[2]
    data = data.reshape((num_samples, num_features, num_nodes, num_nodes))

    # Convert data and labels to TensorFlow tensors
    data = tf.convert_to_tensor(data, dtype=tf.int16)
    labels = tf.convert_to_tensor(labels, dtype=tf.int8)

    return data, labels

# returns data, labels
# where data is a list of length 3 arrays and labels is a list of 10 random values
def load_dummy_data():
    data = []
    labels = []
    for i in range(10):
        # append random 3 length primitive lists with random values in range 1-100, example [3,22,63]
        data.append([random.randint(1,100) for i in range(3)])
        # append random value in range 1-100
        labels.append([random.randint(1,100) for i in range(2)])
    return data, labels

# top of buffer are most recent
def update_replay_buffer():

    #  load used indices from sampled_indices.csv, making empty dataframe if there are no columns
    try:
        sampled_indices = pd.read_csv('sampled_indices.csv') 
    except:
        sampled_indices = pd.DataFrame()

    # Load the data from replay_buffer.csv into dataframe, making empty dataframe if there are no columns
    try:
        old_data = pd.read_csv('replay_buffer.csv')
        old_data = old_data.drop(columns=['Unnamed: 0'], errors='ignore')
        old_points = old_data.drop(sampled_indices['Index'], axis=0)
    except:
        old_points = pd.DataFrame()    
    
    new_data, new_labels = load_gcn_tensors()
    # new_data, new_labels = load_dummy_data()

    if (new_data is None) or (new_labels is None):
        replay_buffer = old_points
    else:
        new_points = pd.DataFrame()
        new_points['Data'] = [data.numpy().astype(np.int16).tolist() for data in new_data]
        new_points['Label'] = new_labels.numpy().astype(np.int8).tolist()

        # append new_data to old_data
        replay_buffer = pd.concat([new_points, old_points], ignore_index=True)
        # remove oldest data if buffer is fulL (points towards end of datframe are oldest)
        if len(replay_buffer) > buffer_size:
            replay_buffer = replay_buffer.head(buffer_size)

    # save replay_buffer to replay_buffer.csv
    replay_buffer.to_csv('replay_buffer.csv')

    # clear sampled_points.csv
    sampled_indices = pd.DataFrame()
    sampled_indices.to_csv('sampled_indices.csv')

def get_sampled_minibatch(minibatch_size=4):
    # Load the data from replay_buffer.csv into dataframe
    replay_buffer = pd.read_csv('replay_buffer.csv')

    # sample minibatch_size indices from replay_buffer
    # sampled_points: points sampled
    # sampled_indices: indices of sampled points
    sampled_points = replay_buffer.sample(minibatch_size)
    sampled_indices = sampled_points.index
    sampled_indices = pd.DataFrame(sampled_indices, columns=['Index'])

    # save sampled_indices (type Index) to sampled_indices.csv
    sampled_indices.to_csv('sampled_indices.csv')

    sampled_points = sampled_points.drop(columns=['Unnamed: 0'], errors='ignore')

    # convert to tensor
    data_list = [np.array(eval(str(data)), dtype=np.int16).reshape(input_shape) for data in sampled_points['Data']]
    label_list = [np.int8(eval(str(label))) for label in sampled_points['Label']]

    # Stack data and labels into separate tensors
    data_tensor = tf.convert_to_tensor(data_list, dtype=tf.int16)
    label_tensor = tf.convert_to_tensor(label_list, dtype=tf.int8)

    return data_tensor, label_tensor

# Set the input shape based on the tensor dimensions
input_shape = (7, 12, 12)
buffer_size = 1000

def training_loop(model, epochs=1, batch_size=16):
    for i in range(epochs):
        print(f"Epoch {i+1}/{epochs}")
        update_replay_buffer()
        data, labels = get_sampled_minibatch(batch_size)
        train_model(model, data, labels, 1, batch_size)
    save_model(model, 'gcn_ver_1.keras')

def test_data():
    # test update_replay_buffer, get_sampled_minibatch
    update_replay_buffer()
    data, labels = get_sampled_minibatch()
    print('data: ', data)
    print('labels: ', labels)
    print('data shape:', data.shape)
    print('labels shape:', labels.shape)

if __name__ == "__main__":
    
    # update_replay_buffer()

    # If file 'gcn_ver_1.keras' exists, load the model from the file
    # Otherwise, create a new model
    try:
        model = keras.models.load_model('gcn_ver_1.keras')
    except:
        model = create_gcn_model(input_shape)

    training_loop(model)
    # # Evaluate the model
    # evaluate_model(model, data, labels)
