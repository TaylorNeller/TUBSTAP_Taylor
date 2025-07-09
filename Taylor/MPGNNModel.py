from NNModel import NNModel
import tensorflow as tf
from tensorflow import keras
from tensorflow.keras import layers
from spektral.layers import MessagePassing

# Custom message-passing layer with edge features
class EdgeMPNNLayer(MessagePassing):
    def __init__(self, channels, **kwargs):
        super().__init__(aggregate="sum", **kwargs)
        self.channels = channels
        self.dense_update = layers.Dense(channels)
        self.edge_mlp = layers.Dense(channels)

    def message(self, x, edge_index, edge_features):
        edge_messages = self.edge_mlp(edge_features)
        source_nodes = tf.gather(x, edge_index[0])
        messages = source_nodes + edge_messages
        return messages

    def update(self, embeddings, x):
        return self.dense_update(embeddings) + x

# Custom layer to convert adjacency matrix to sparse format
class SparseConversionLayer(layers.Layer):
    def call(self, inputs):
        return tf.sparse.from_dense(inputs)

# MP-GNN Model with Edge Features and Sparse Conversion
class MPGNNModel(NNModel):
    def __init__(self, model_name):
        super().__init__(model_name)
        self.model_type = "MPGNN"
        self.num_nodes = 6
        self.num_node_features = 4
        self.num_edge_features = 3

        self.mpnn_layers = [16, 4]
        self.dense_layers = [144, 512]

    def create_model(self):
        adjacency_shape = (self.num_nodes, self.num_nodes)
        feature_shape = (self.num_nodes, self.num_node_features)

        # Define inputs
        adjacency_input = keras.Input(shape=adjacency_shape)
        feature_input = keras.Input(shape=feature_shape)
        edge_feature_inputs = [keras.Input(shape=adjacency_shape) for _ in range(self.num_edge_features)]

        # Concatenate edge features
        if self.num_edge_features > 1:
            concatenated_edge_features = layers.Concatenate(axis=-1)(edge_feature_inputs)
        else:
            concatenated_edge_features = edge_feature_inputs[0]

        # Convert adjacency matrix to sparse format using the custom layer
        sparse_adjacency_input = SparseConversionLayer()(adjacency_input)

        # Message Passing Layers
        x = EdgeMPNNLayer(self.mpnn_layers[0])([feature_input, sparse_adjacency_input, concatenated_edge_features])
        x = EdgeMPNNLayer(self.mpnn_layers[1])([x, sparse_adjacency_input, concatenated_edge_features])

        # Flatten and Dense Layers
        x = layers.Flatten()(x)
        x = layers.Dense(self.dense_layers[0], activation="relu")(x)
        x = layers.Dense(self.dense_layers[1], activation="relu")(x)
        outputs = layers.Dense(1, activation="tanh")(x)

        # Define model
        model = keras.Model(inputs=[adjacency_input, feature_input] + edge_feature_inputs, outputs=outputs)

        # Compile the model
        optimizer = keras.optimizers.Adam(learning_rate=self.learning_rate, clipnorm=1.0)
        model.compile(optimizer=optimizer, loss="mse", metrics=self.metric)

        self.active_model = model
        return model

    def select_data(self, tensors):
        return tensors[:5]
