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
from DataManager import DataManager
import os
import pickle
from sklearn.preprocessing import StandardScaler


model_fname = './data/gcn_ver_2.keras'
scaler_dir = './data/'

# input shapes
num_nodes = 12
num_node_features = 4
num_edge_features = 3 

adjacency_shape = (num_nodes, num_nodes)
feature_shape = (num_nodes, num_node_features)

feature_scalers = None
edge_feature_scalers = None

@tf.keras.utils.register_keras_serializable()
class MaskConversionLayer(layers.Layer):
    def __init__(self, **kwargs):
        super(MaskConversionLayer, self).__init__(**kwargs)

    def call(self, inputs):
        return tf.cast(inputs, tf.float32)

    def compute_output_shape(self, input_shape):
        return input_shape

@tf.keras.utils.register_keras_serializable()
class EdgeMaskLayer(layers.Layer):
    def __init__(self, **kwargs):
        super(EdgeMaskLayer, self).__init__(**kwargs)

    def call(self, inputs):
        return tf.expand_dims(inputs, axis=-1) * tf.expand_dims(inputs, axis=-2)

    def compute_output_shape(self, input_shape):
        return (input_shape[0], input_shape[0])

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
    # node_mask = Lambda(lambda x: tf.cast(x, tf.float32))(mask_input)
    # edge_mask = Lambda(lambda x: tf.cast(tf.expand_dims(x, axis=-1) * tf.expand_dims(x, axis=-2), tf.float32))(mask_input)
    # node_mask = Lambda(lambda x: tf.cast(x, tf.float32), output_shape=(num_nodes,))(mask_input)
    # edge_mask = Lambda(lambda x: tf.cast(tf.expand_dims(x, axis=-1) * tf.expand_dims(x, axis=-2), tf.float32),
    #                output_shape=(num_nodes, num_nodes))(mask_input)
    
    node_mask = MaskConversionLayer()(mask_input)
    node_mask = Reshape((num_nodes, 1))(node_mask)  # Reshape node mask to match the GCN output shape
    edge_mask = MaskConversionLayer()(EdgeMaskLayer()(mask_input))

    x = GCNConv(128, activation="relu")([feature_input, adjacency_input], mask=[node_mask, edge_mask])
    x = layers.BatchNormalization()(x)
    x = layers.Concatenate(axis=-1)([x, concatenated_edge_features])
    x = GCNConv(64, activation="relu")([x, adjacency_input], mask=[node_mask, edge_mask])
    x = layers.BatchNormalization()(x)
    x = layers.GlobalAveragePooling1D()(x)
    x = layers.Dense(64, activation="relu")(x)
    outputs = layers.Dense(3, activation="softmax")(x)

    model = keras.Model(inputs=[adjacency_input, feature_input] + edge_feature_inputs + [mask_input], outputs=outputs)

    # Compile the model
    optimizer = keras.optimizers.Adam(learning_rate=0.001, clipvalue=1.0)
    model.compile(optimizer=optimizer, loss="sparse_categorical_crossentropy", metrics=["accuracy"])

    #save the model
    save_model(model, model_fname) 

    return model

def evaluate_model(model, batch_size):
    # Evaluate the model
    load_scalers()
    DataManager.update_replay_buffer(batch_size)
    data, labels = DataManager.get_sampled_minibatch(batch_size)
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


def convert_to_tensors(data_column, label_column):
    global feature_scalers, edge_feature_scalers

    if feature_scalers is None or edge_feature_scalers is None:
        raise ValueError("Scalers are not loaded. Please call load_scalers() first.")
    
    # Prepare the data
    adjacency_matrices = []
    feature_matrices = []
    edge_feature_matrices = [[] for _ in range(3)]  # Assuming 3 edge features
    masks = []

    for _, data_point in data_column.iterrows():
        adjacency_matrix = np.array(eval(data_point['Adjacency']))
        feature_matrix = np.array(eval(data_point['Feature']))
        edge_features = [np.array(eval(data_point[f'EdgeFeature{i+1}'])) for i in range(3)]

        # Scale each column of the feature matrix
        for i in range(num_node_features):
            feature_matrix[:, i] = feature_scalers[i].transform(feature_matrix[:, i].reshape(-1, 1)).flatten()

        # Scale the edge features
        for i, edge_feature in enumerate(edge_features):
            edge_features[i] = edge_feature_scalers[i].transform(edge_feature)

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
    

    # Transform labels from -1, 0, 1 to 0, 1, 2
    transformed_labels = label_column + 1
    labels = tf.convert_to_tensor(transformed_labels.tolist(), dtype=tf.float32)

    masks = tf.convert_to_tensor(masks, dtype=tf.float32)

    return [adjacency_matrices, feature_matrices, edge_feature_tensors, masks, labels]


def fit_scalers(data_column):
    # Initialize a scaler for each column in the feature matrix
    feature_scalers = [StandardScaler() for _ in range(num_node_features)]
    edge_feature_scalers = [StandardScaler() for _ in range(num_edge_features)]

    # Collect all features and edge features to fit scalers
    all_features = [[] for _ in range(num_node_features)]
    all_edge_features = [[] for _ in range(num_edge_features)]

    for _, data_point in data_column.iterrows():
        feature_matrix = np.array(eval(data_point['Feature']))
        edge_features = [np.array(eval(data_point[f'EdgeFeature{i+1}'])) for i in range(num_edge_features)]

        for i in range(num_node_features):
            all_features[i].append(feature_matrix[:, i])
        
        for i in range(num_edge_features):
            all_edge_features[i].append(edge_features[i])

    # Fit scalers
    for i in range(num_node_features):
        all_features[i] = np.concatenate(all_features[i])
        feature_scalers[i].fit(all_features[i].reshape(-1, 1))

    for i in range(num_edge_features):
        all_edge_features[i] = np.vstack(all_edge_features[i])
        edge_feature_scalers[i].fit(all_edge_features[i])

    # Save scalers
    with open(f'{scaler_dir}feature_scalers.pkl', 'wb') as f:
        pickle.dump(feature_scalers, f)
    
    for i in range(num_edge_features):
        with open(f'{scaler_dir}edge_feature_scaler_{i}.pkl', 'wb') as f:
            pickle.dump(edge_feature_scalers[i], f)

    print("Scalers fitted and saved.")

def load_scalers():
    global feature_scalers, edge_feature_scalers
    with open(f'{scaler_dir}feature_scalers.pkl', 'rb') as f:
        feature_scalers = pickle.load(f)
    
    edge_feature_scalers = []
    for i in range(num_edge_features):
        with open(f'{scaler_dir}edge_feature_scaler_{i}.pkl', 'rb') as f:
            edge_feature_scalers.append(pickle.load(f))
    print("Scalers loaded.")

def train_model(model, data, epochs, batch_size):
    adjacency_matrices = data[0]
    feature_matrices = data[1]
    edge_feature_tensors = data[2]
    masks = data[3]
    labels = data[4]

    # print('fitting model...')
    # Train the model
    model.fit([adjacency_matrices, feature_matrices] + edge_feature_tensors + [masks],
              labels, epochs=epochs, batch_size=batch_size)

def training_loop(model, epochs, batch_size):
    DataManager.minibatch_size = batch_size
    load_scalers()

    for i in range(epochs):
        print(f"Epoch {i+1}/{epochs}")
        DataManager.update_replay_buffer()
        # print('finished updating buffer')
        data, labels = DataManager.get_sampled_minibatch()
        input_data = convert_to_tensors(data, labels)
        train_model(model, input_data, 1, batch_size)

    print('saving model...')
    save_model(model, model_fname)

def test_data():
    # test update_replay_buffer, get_sampled_minibatch
    DataManager.update_replay_buffer()
    data, labels = DataManager.get_sampled_minibatch()
    print('data: ', data)
    print('labels: ', labels)
    print('data shape:', data.shape)
    print('labels shape:', labels.shape)

if __name__ == "__main__":
    # fit scalers
    # data, labels = DataManager.get_sampled_minibatch(1000, False)
    # fit_scalers(data)

    # delete old model
    # if os.path.exists(model_fname):
    #     os.remove(model_fname)

    # DataManager.verbose = True
    # DataManager.update_replay_buffer(1)

    # If file {model_fname} exists, load the model from the file
    # Otherwise, create a new model
    keras.config.enable_unsafe_deserialization()
    try:
        model = keras.models.load_model(model_fname, custom_objects={'MaskConversionLayer': MaskConversionLayer, 'edge_mask_function': EdgeMaskLayer})
        print('loaded saved model')
    except Exception as e:
        # print exception
        print(e)
        model = create_gcn_model(adjacency_shape, feature_shape, num_edge_features)
        print('created new model')

    DataManager.update_size = 32
    training_loop(model, 300, 32)

    # Evaluate the model
    # evaluate_model(model,100)