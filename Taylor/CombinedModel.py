from NNModel import NNModel
import tensorflow as tf
from tensorflow import keras
from tensorflow.keras import layers
from tensorflow.keras.models import Model
from spektral.layers import GCNConv


# child of NNModel
class CombinedModel(NNModel):
    def __init__(self, model_name):
        super().__init__(model_name)
        self.model_type = "Combined"

        self.num_cnn_inputs = 5
        self.map_x = 8
        self.map_y = 8

        self.num_nodes = 6
        self.num_node_features = 4
        self.num_edge_features = 3 

        self.gcn_layers = [16, 4]
        self.dense_layers = [144, 512]
        

    def create_model(self):
        # GCN inputs
        adjacency_shape = (self.num_nodes, self.num_nodes)
        feature_shape = (self.num_nodes, self.num_node_features)
        
        adjacency_input = keras.Input(shape=adjacency_shape)
        feature_input = keras.Input(shape=feature_shape)
        edge_feature_inputs = [keras.Input(shape=adjacency_shape) for _ in range(self.num_edge_features)]
        
        # CNN inputs
        cnn_inputs = [
            layers.Input(shape=(self.map_x, self.map_y)) for _ in range(self.num_cnn_inputs)
        ]
        
        # GCN branch
        if self.num_edge_features > 1:
            concatenated_edge_features = layers.Concatenate(axis=-1)(edge_feature_inputs)
        else:
            concatenated_edge_features = edge_feature_inputs[0]
            
        x_gcn = GCNConv(self.gcn_layers[0], activation="relu")([feature_input, adjacency_input])
        x_gcn = layers.Concatenate(axis=-1)([x_gcn, concatenated_edge_features])
        x_gcn = GCNConv(self.gcn_layers[1], activation="relu")([x_gcn, adjacency_input])
        x_gcn = layers.Flatten()(x_gcn)
        x_gcn = layers.Dense(self.dense_layers[0], activation="relu")(x_gcn)
        x_gcn = layers.Dense(self.dense_layers[1], activation="relu")(x_gcn)
        
        # CNN branch
        x_cnn = tf.stack(cnn_inputs, axis=-1)
        
        x_cnn = layers.Conv2D(32, (3, 3), padding='same', activation='relu')(x_cnn)
        x_cnn = layers.BatchNormalization()(x_cnn)
        x_cnn = layers.Conv2D(32, (3, 3), padding='same', activation='relu')(x_cnn)
        x_cnn = layers.BatchNormalization()(x_cnn)
        x_cnn = layers.MaxPooling2D((2, 2))(x_cnn)
        
        x_cnn = layers.Conv2D(64, (3, 3), padding='same', activation='relu')(x_cnn)
        x_cnn = layers.BatchNormalization()(x_cnn)
        x_cnn = layers.Conv2D(64, (3, 3), padding='same', activation='relu')(x_cnn)
        x_cnn = layers.BatchNormalization()(x_cnn)
        x_cnn = layers.MaxPooling2D((2, 2))(x_cnn)
        
        x_cnn = layers.Flatten()(x_cnn)
        x_cnn = layers.Dense(128, activation='relu')(x_cnn)
        x_cnn = layers.BatchNormalization()(x_cnn)
        x_cnn = layers.Dropout(0.5)(x_cnn)
        x_cnn = layers.Dense(64, activation='relu')(x_cnn)
        x_cnn = layers.BatchNormalization()(x_cnn)
        x_cnn = layers.Dropout(0.3)(x_cnn)
        
        # Combine both branches
        combined = layers.Concatenate()([x_gcn, x_cnn])
        
        # Final dense layers
        x = layers.Dense(256, activation='relu')(combined)
        x = layers.BatchNormalization()(x)
        x = layers.Dropout(0.3)(x)
        outputs = layers.Dense(1, activation='tanh')(x)
        
        # Create model with all inputs
        model = Model(
            inputs=[adjacency_input, feature_input] + edge_feature_inputs + cnn_inputs,
            outputs=outputs
        )
        
        # Compile the model
        optimizer = keras.optimizers.Adam(learning_rate=self.learning_rate, clipnorm=1.0)
        model.compile(optimizer=optimizer, loss="mse", metrics=self.metric)
        
        self.active_model = model
        return model
    
    def select_data(self, tensors):
        return tensors