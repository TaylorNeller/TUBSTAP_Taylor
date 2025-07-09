from tensorflow import keras
from tensorflow.keras import layers
from spektral.layers import GCNConv, GATConv
from spektral.layers.pooling.global_pool import GlobalAvgPool, GlobalMaxPool
from NNModel import NNModel

class ImprovedGNNModel(NNModel):
    def __init__(self, model_name):
        super().__init__(model_name)
        self.model_type = "ImprovedGNN"
        
        self.num_nodes = 6
        self.num_node_features = 4
        self.num_edge_features = 3
        
        # Updated layer configurations
        self.gcn_layers = [32, 16]
        self.dense_layers = [256, 128]

    def create_model(self):
        # Define input shapes
        adjacency_shape = (self.num_nodes, self.num_nodes)
        feature_shape = (self.num_nodes, self.num_node_features)
        
        # Define inputs
        adjacency_input = keras.Input(shape=adjacency_shape)
        feature_input = keras.Input(shape=feature_shape)
        edge_feature_inputs = [keras.Input(shape=adjacency_shape) for _ in range(self.num_edge_features)]
        
        # Concatenate the edge features
        concatenated_edge_features = layers.Concatenate(axis=-1)(edge_feature_inputs)
        
        # GCN Layers with attention
        x = GATConv(self.gcn_layers[0], attn_heads=1, activation="relu", kernel_initializer='glorot_uniform')([feature_input, adjacency_input])
        x = layers.Concatenate(axis=-1)([x, concatenated_edge_features])
        x = GATConv(self.gcn_layers[1], attn_heads=1, activation="relu", kernel_initializer='glorot_uniform')([x, adjacency_input])
        
        # Pooling to reduce dimensions
        x = GlobalAvgPool()(x)
        
        # Dense layers with improved structure
        x = layers.Dense(self.dense_layers[0], activation="relu")(x)
        x = layers.Dropout(0.3)(x)  # Regularization
        x = layers.Dense(self.dense_layers[1], activation="relu")(x)
        x = layers.Dropout(0.3)(x)  # Regularization
        
        # Final output layer
        outputs = layers.Dense(1, activation="tanh")(x)
        
        # Model definition
        model = keras.Model(inputs=[adjacency_input, feature_input] + edge_feature_inputs, outputs=outputs)
        
        # Compile the model with optimizer
        optimizer = keras.optimizers.Adam(learning_rate=self.learning_rate, clipnorm=1.0)
        model.compile(optimizer=optimizer, loss="mse", metrics=self.metric)
        
        self.active_model = model
        return model

    # Selecting the appropriate data tensors
    def select_data(self, tensors):
        return tensors[:5]
