import Spec
import sys
from PyQt5.QtWidgets import QWidget, QApplication, QLabel, QVBoxLayout
from PyQt5.QtGui import QPixmap, QPainter, QColor, QFont, QImage
from PyQt5.QtCore import Qt

image_folder = "../bin/Release/img/"

class DrawManager:
    BMP_SIZE = 48  # Size of each cell

    def __init__(self, form, picture_box):
        self.unit_bmps = None  # Bitmap of units for each color and type
        self.field_bmps = None  # Bitmap for each terrain type
        self.number_bmps = None  # Font for displaying remaining HP
        self.character_bmps = None  # Font for displaying unit type
        self.end_mark_bmp = None  # Bitmap for displaying the end mark and movable range of units
        self.red_window_bmp = None
        self.blue_window_bmp = None
        self.ia_05_alpha = None  # Image attributes for filters
        self.ia_dark = None
        self.graphics = None  # Drawing handler (obtained from picture_box)
        self.form = form  # Parent form, used for refresh
        self.picture_box = picture_box  # Target for drawing
        self.map = None  # Temporary information
        self.can_be_attacked = None  # If the flag at the corresponding index is set, a red frame is displayed in the selectable area
        self.red_windowed = None  # If the corresponding position is true, a red frame is displayed
        self.ai_set_string = None  # Array for AI, any string can be displayed
        self.font = QFont("Times New Roman", 10, QFont.Bold)
        self.brush = QColor(255, 255, 255, 255)
        
        self.load_bitmaps()

    def load_bitmaps(self):
        # Load bitmaps for terrain and units from the "img" directory
        self.unit_bmps = [[QPixmap(f"{image_folder}{color}_{unit_type}.png") for unit_type in Spec.SPEC_NAMES] for color in ["Red", "Blue"]]
        self.field_bmps = [QPixmap(f"{image_folder}Field_{field_type}.png") for field_type in ["noentry", "plain", "sea", "wood", "mountain", "road", "fortress"]]
        self.number_bmps = [QPixmap(f"{image_folder}Number_{i}.png") for i in range(11)]
        self.character_bmps = [QPixmap(f"{image_folder}Char_{char}.png") for char in ["F", "A", "P", "U", "R", "I"]]
        self.end_mark_bmp = QPixmap(f"{image_folder}Mark_end.png")
        self.red_window_bmp = QPixmap(f"{image_folder}Mark_redselect.png")
        self.blue_window_bmp = QPixmap(f"{image_folder}Mark_blueselect.png")

        # Set variables for filters
        self.ia_05_alpha = QImage.Format_ARGB32
        self.ia_dark = QImage.Format_ARGB32

    def set_map(self, map):
        self.map = map
        self.can_be_attacked = [[False] * map.get_y_size() for _ in range(map.get_x_size())]  # Array containing flags for displaying blue frame
        self.red_windowed = [[False] * map.get_y_size() for _ in range(map.get_x_size())]  # Array containing flags for displaying red frame
        self.ai_set_string = [[None] * map.get_y_size() for _ in range(map.get_x_size())]

    def draw_map(self):
        if self.map is None:
            return

        # Draw each cell
        for x in range(1, self.map.get_x_size() - 1):
            for y in range(1, self.map.get_y_size() - 1):
                self.draw_cell(x, y)

    def draw_cell(self, x, y):
        unit = self.map.get_unit_at(x, y)
        field_type = self.map.get_field_type(x, y)

        # Draw terrain
        self.graphics.drawPixmap(self.BMP_SIZE * (x - 1), self.BMP_SIZE * (y - 1), self.field_bmps[field_type])

        if unit is not None:
            # Draw unit
            if unit.is_action_finished():
                # Draw unit with finished action
                self.graphics.drawPixmap(self.BMP_SIZE * (x - 1), self.BMP_SIZE * (y - 1), self.unit_bmps[unit.get_team_color()][unit.get_type_of_unit()], self.ia_dark)
                self.graphics.drawPixmap(self.BMP_SIZE * (x - 1), self.BMP_SIZE * (y - 1), self.end_mark_bmp)
            else:
                # Draw unit before action
                self.graphics.drawPixmap(self.BMP_SIZE * (x - 1), self.BMP_SIZE * (y - 1), self.unit_bmps[unit.get_team_color()][unit.get_type_of_unit()])

            # Display unit's HP
            self.graphics.drawPixmap(self.BMP_SIZE * (x - 1), self.BMP_SIZE * (y - 1), self.number_bmps[unit.get_HP()])

            # Display unit's initial character
            self.graphics.drawPixmap(self.BMP_SIZE * (x - 1), self.BMP_SIZE * (y - 1), self.character_bmps[unit.get_spec().get_unit_type()])

        # Draw red frame if the unit's selectable flag is set
        if self.can_be_attacked is not None and self.can_be_attacked[x][y]:
            self.graphics.drawPixmap(self.BMP_SIZE * (x - 1), self.BMP_SIZE * (y - 1), self.red_window_bmp, self.ia_05_alpha)

        # Display blue frame in movable range
        if self.red_windowed is not None and self.red_windowed[x][y]:
            self.graphics.drawPixmap(self.BMP_SIZE * (x - 1), self.BMP_SIZE * (y - 1), self.blue_window_bmp, self.ia_05_alpha)

        if self.ai_set_string is not None and self.ai_set_string[x][y] is not None:
            self.graphics.drawText(self.BMP_SIZE * (x - 1) + 1, self.BMP_SIZE * (y - 1) + 1, self.ai_set_string[x][y])
            self.graphics.setPen(Qt.white)
            self.graphics.drawText(self.BMP_SIZE * (x - 1), self.BMP_SIZE * (y - 1), self.ai_set_string[x][y])
            self.graphics.setPen(Qt.black)

    def redraw_map(self, map):
        self.map = map
        self.draw_map()
        self.form.update()

    def redraw_map_with_range(self, map, select_range):
        self.map = map
        self.set_map(self.map)
        self.draw_map()
        self.form.update()

    def show_unit_movable_pos(self, range, map):
        for x in range(map.get_x_size()):
            for y in range(map.get_y_size()):
                if range[x][y]:
                    self.red_windowed[x][y] = True  # Set flag to draw movable range in blue

        self.redraw_map(map)
        self.init_bool_array()

    def show_attack_target_units(self, map, can_attack_units):
        for unit in can_attack_units:
            x = unit.get_x_pos()
            y = unit.get_y_pos()
            self.can_be_attacked[x][y] = True  # Set flag to draw red frame as attackable position

        self.redraw_map(map)
        self.init_bool_array()

    def init_bool_array(self):
        for x in range(self.map.get_x_size()):
            for y in range(self.map.get_y_size()):
                self.can_be_attacked[x][y] = False
                self.red_windowed[x][y] = False

    def clear_map_image(self):
        self.picture_box.setPixmap(QPixmap(self.picture_box.width(), self.picture_box.height()))
        self.graphics = QPainter(self.picture_box.pixmap())

    @staticmethod
    def draw_string_on_map(value):
        DrawManager.ai_set_string = value
        DrawManager.form.update()


class MainWindow(QWidget):
    def __init__(self):
        super().__init__()
        self.setWindowTitle("Map Display")
        self.setGeometry(100, 100, 800, 600)

        self.picture_box = QLabel(self)
        self.picture_box.setGeometry(0, 0, 800, 600)

        self.draw_manager = DrawManager(self, self.picture_box)

        layout = QVBoxLayout()
        layout.addWidget(self.picture_box)
        self.setLayout(layout)


if __name__ == '__main__':
    app = QApplication(sys.argv)
    window = MainWindow()
    window.show()
    sys.exit(app.exec_())