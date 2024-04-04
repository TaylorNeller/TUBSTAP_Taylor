import tensorflow as tf
import Preprocess
from tensorflow import keras
from tensorflow.keras import layers
from tensorflow.keras.models import save_model
from keras.layers import Lambda, Reshape
from spektral.layers import GCNConv
import pandas as pd
import numpy as np
import random

def create_gcn_model(adjacency_shape, feature_shape, num_edge_features):
    # Define the GCN model architecture
    adjacency_input = keras.Input(shape=adjacency_shape)
    feature_input = keras.Input(shape=feature_shape)
    edge_feature_inputs = [keras.Input(shape=adjacency_shape) for _ in range(num_edge_features)]
    mask_input = keras.Input(shape=(num_nodes,), dtype=tf.float32)
    
    # Concatenate the edge feature matrices
    if num_edge_features > 1:
        concatenated_edge_features = layers.Concatenate(axis=-1)(edge_feature_inputs)
    else:
        concatenated_edge_features = edge_feature_inputs[0]
    
    # Convert mask to float32 using a Lambda layer
    node_mask = Lambda(lambda x: tf.cast(x, tf.float32))(mask_input)
    node_mask = Reshape((num_nodes, 1))(node_mask)  # Reshape node mask to match the GCN output shape
    edge_mask = Lambda(lambda x: tf.cast(tf.expand_dims(x, axis=-1) * tf.expand_dims(x, axis=-2), tf.float32))(mask_input)
    
    x = GCNConv(32, activation="relu")([feature_input, adjacency_input], mask=[node_mask, edge_mask])
    x = layers.Concatenate(axis=-1)([x, concatenated_edge_features])
    x = GCNConv(64, activation="relu")([x, adjacency_input], mask=[node_mask, edge_mask])
    x = layers.GlobalAveragePooling1D()(x)
    x = layers.Dense(128, activation="relu")(x)
    outputs = layers.Dense(1, activation="tanh")(x)

    model = keras.Model(inputs=[adjacency_input, feature_input] + edge_feature_inputs + [mask_input], outputs=outputs)

    # Compile the model
    model.compile(optimizer="adam", loss="mse", metrics=["accuracy"])

    return model

def evaluate_model(model, batch_size):
    # Evaluate the model
    update_replay_buffer()
    data, labels = get_sampled_minibatch(batch_size)
    tensors = convert_to_tensors(data, labels)
    adjacency_matrices = tensors[0]
    feature_matrices = tensors[1]
    edge_feature_tensors = tensors[2]
    masks = tensors[3]
    labels = tensors[4]



    loss, accuracy = model.evaluate([adjacency_matrices, feature_matrices] + edge_feature_tensors + [masks],
                                    labels)
    print(f"Test Loss: {loss:.4f}")
    print(f"Test Accuracy: {accuracy:.4f}")

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
        old_points = old_data.drop(columns=['Unnamed: 0'], errors='ignore')
        if len(sampled_indices) > 0:
            old_points = old_points.drop(sampled_indices['Index'], axis=0)
    except:
        old_points = pd.DataFrame()    
    
    new_data, new_labels = Preprocess.load_gcn_matrices()
    # new_data, new_labels = load_dummy_data()

    # print('new_data:', new_data)
    # print('new_labels:', new_labels)

    if (new_data is None) or (new_labels is None):
        replay_buffer = old_points
    else:
        new_points = pd.DataFrame()
        new_points['Adjacency'] = [data_point[0] for data_point in new_data]
        new_points['Feature'] = [data_point[1] for data_point in new_data]
        new_points['EdgeFeature1'] = [data_point[2][0] for data_point in new_data]
        new_points['EdgeFeature2'] = [data_point[2][1] for data_point in new_data]
        new_points['EdgeFeature3'] = [data_point[2][2] for data_point in new_data]
        new_points['Label'] = new_labels

        # append new_data to old_data
        replay_buffer = pd.concat([new_points, old_points], ignore_index=True)
        # remove oldest data if buffer is full (points towards end of dataframe are oldest)
        if len(replay_buffer) > buffer_size:
            replay_buffer = replay_buffer.head(buffer_size)

    # save replay_buffer to replay_buffer.csv
    replay_buffer.to_csv('replay_buffer.csv')

    # clear sampled_points.csv
    sampled_indices = pd.DataFrame()
    sampled_indices.to_csv('sampled_indices.csv')

def get_sampled_minibatch(minibatch_size):
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
    sampled_data = sampled_points.drop(columns=['Label'])
    label_column = sampled_points['Label']

    return sampled_data, label_column

def convert_to_tensors(data_column, label_column):
    # Prepare the data
    adjacency_matrices = []
    feature_matrices = []
    edge_feature_matrices = [[] for _ in range(3)]  # Assuming 3 edge features
    masks = []

    for _, data_point in data_column.iterrows():
        adjacency_matrix = np.array(eval(data_point['Adjacency']))
        feature_matrix = np.array(eval(data_point['Feature']))
        edge_features = [np.array(eval(data_point[f'EdgeFeature{i+1}'])) for i in range(3)]

        mask = np.array([1.0 if feature_matrix[node_id][0] != 0 else 0.0 for node_id in range(num_nodes)])

        adjacency_matrices.append(adjacency_matrix)
        feature_matrices.append(feature_matrix)
        for i, edge_feature in enumerate(edge_features):
            edge_feature_matrices[i].append(edge_feature)
        masks.append(mask)


    # Convert data to TensorFlow tensors
    adjacency_matrices = tf.convert_to_tensor(adjacency_matrices, dtype=tf.float32)
    feature_matrices = tf.convert_to_tensor(feature_matrices, dtype=tf.float32)
    edge_feature_tensors = [tf.convert_to_tensor(edge_feature_matrix, dtype=tf.float32)
                            for edge_feature_matrix in edge_feature_matrices]
    labels = tf.convert_to_tensor(label_column.tolist(), dtype=tf.float32)
    masks = tf.convert_to_tensor(masks, dtype=tf.float32)

    return [adjacency_matrices, feature_matrices, edge_feature_tensors, masks, labels]

def train_model(model, data, epochs, batch_size):
    adjacency_matrices = data[0]
    feature_matrices = data[1]
    edge_feature_tensors = data[2]
    masks = data[3]
    labels = data[4]

    # Train the model
    model.fit([adjacency_matrices, feature_matrices] + edge_feature_tensors + [masks],
              labels, epochs=epochs, batch_size=batch_size)

# Set the input shape based on the tensor dimensions
buffer_size = 1000

def training_loop(model, epochs, batch_size):
    for i in range(epochs):
        print(f"Epoch {i+1}/{epochs}")
        update_replay_buffer()
        data, labels = get_sampled_minibatch(batch_size)
        input_data = convert_to_tensors(data, labels)
        train_model(model, input_data, 1, batch_size)
    save_model(model, 'gcn_ver_1.keras')

def test_data():
    # test update_replay_buffer, get_sampled_minibatch
    update_replay_buffer()
    data, labels = get_sampled_minibatch()
    print('data: ', data)
    print('labels: ', labels)
    print('data shape:', data.shape)
    print('labels shape:', labels.shape)

# input shapes
num_nodes = 12
num_node_features = 4
num_edge_features = 3 

adjacency_shape = (num_nodes, num_nodes)
feature_shape = (num_nodes, num_node_features)



if __name__ == "__main__":
    
    # update_replay_buffer()

    # If file 'gcn_ver_1.keras' exists, load the model from the file
    # Otherwise, create a new model
    try:
        model = keras.models.load_model('gcn_ver_1.keras')
    except:
        model = create_gcn_model(adjacency_shape, feature_shape, num_edge_features)


    training_loop(model, 1, 8)

    # # Evaluate the model
    evaluate_model(model, 8)
