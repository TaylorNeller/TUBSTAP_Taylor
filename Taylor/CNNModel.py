from NNModel import NNModel
import tensorflow as tf
from tensorflow import keras
from tensorflow.keras import layers
from tensorflow.keras.models import Model


# child of NNModel
class CNNModel(NNModel):
    def __init__(self, model_name):
        super().__init__(model_name)
        self.model_type = "CNN"

        self.num_cnn_inputs = 5
        self.map_x = 8
        self.map_y = 8
        

    def create_model(self):
        input_layers = [
            layers.Input(shape=(self.map_x, self.map_y)) for _ in range(self.num_cnn_inputs)
        ]
        
        # Stack inputs into a single tensor
        x = tf.stack(input_layers, axis=-1)
        
        # First convolutional block
        x = layers.Conv2D(32, (3, 3), padding='same', activation='relu')(x)
        x = layers.BatchNormalization()(x)
        x = layers.Conv2D(32, (3, 3), padding='same', activation='relu')(x)
        x = layers.BatchNormalization()(x)
        x = layers.MaxPooling2D((2, 2))(x)
        
        # Second convolutional block
        x = layers.Conv2D(64, (3, 3), padding='same', activation='relu')(x)
        x = layers.BatchNormalization()(x)
        x = layers.Conv2D(64, (3, 3), padding='same', activation='relu')(x)
        x = layers.BatchNormalization()(x)
        x = layers.MaxPooling2D((2, 2))(x)
        
        # Flatten and dense layers
        x = layers.Flatten()(x)
        x = layers.Dense(128, activation='relu')(x)
        x = layers.BatchNormalization()(x)
        x = layers.Dropout(0.5)(x)
        x = layers.Dense(64, activation='relu')(x)
        x = layers.BatchNormalization()(x)
        x = layers.Dropout(0.3)(x)
        
        # Output layer
        outputs = layers.Dense(1, activation='tanh')(x)
        
        # Create model with multiple inputs
        model = Model(inputs=input_layers, outputs=outputs)
        
        # Compile the model
        optimizer = keras.optimizers.Adam(learning_rate=self.learning_rate, clipnorm=1.0)
        model.compile(optimizer=optimizer, loss="mse", metrics=self.metric)
        
        self.active_model = model
        return model
    
    # takes list style data=[adjacency, feature, [ef1,ef2,ef3], ...], labels
    # def tensorize_data(self, data, labels):
    #     map_mat = tf.convert_to_tensor(data[5], dtype=tf.float32)
    #     type_mat = tf.convert_to_tensor(data[6], dtype=tf.float32)
    #     team_mat = tf.convert_to_tensor(data[7], dtype=tf.float32)
    #     hp_mat = tf.convert_to_tensor(data[8], dtype=tf.float32)
    #     moved_mat = tf.convert_to_tensor(data[9], dtype=tf.float32)
    #     labels = tf.convert_to_tensor(labels, dtype=tf.float32)

    #     return [map_mat, type_mat, team_mat, hp_mat, moved_mat], labels

    def select_data(self, tensors):
        return tensors[5:10]