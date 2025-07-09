import tkinter as tk
from tkinter import messagebox

class GameBoardGUI:
    def __init__(self, x_size=8, y_size=8):
        self.root = tk.Tk()
        self.root.title("Game Board")
        
        self.x_size = x_size
        self.y_size = y_size
        self.cell_size = 60
        
        # Variables to store the move coordinates
        self.start_x = None
        self.start_y = None
        self.end_x = None
        self.end_y = None
        self.final_x = None
        self.final_y = None
        
        # Flag to track drag state
        self.dragging = False
        self.drag_complete = False
        
        # Create canvas
        self.canvas = tk.Canvas(
            self.root,
            width=self.x_size * self.cell_size,
            height=self.y_size * self.cell_size,
            bg='white'
        )
        self.canvas.pack(padx=10, pady=10)
        
        # Bind mouse events
        self.canvas.bind('<Button-1>', self.on_click)
        self.canvas.bind('<B1-Motion>', self.on_drag)
        self.canvas.bind('<ButtonRelease-1>', self.on_release)
        
        # Draw initial grid
        self.draw_grid()
        
        # Store cells for highlighting
        self.cells = {}
        for y in range(self.y_size):
            for x in range(self.x_size):
                x1 = x * self.cell_size
                y1 = y * self.cell_size
                x2 = x1 + self.cell_size
                y2 = y1 + self.cell_size
                self.cells[(x, y)] = self.canvas.create_rectangle(
                    x1, y1, x2, y2,
                    fill='white',
                    outline='black'
                )
        
        self.result = None
    
    def draw_grid(self):
        # Draw vertical lines
        for x in range(self.x_size + 1):
            self.canvas.create_line(
                x * self.cell_size, 0,
                x * self.cell_size, self.y_size * self.cell_size,
                fill='black'
            )
        
        # Draw horizontal lines
        for y in range(self.y_size + 1):
            self.canvas.create_line(
                0, y * self.cell_size,
                self.x_size * self.cell_size, y * self.cell_size,
                fill='black'
            )
    
    def get_cell_coords(self, event):
        x = event.x // self.cell_size
        y = event.y // self.cell_size
        return x, y
    
    def highlight_cell(self, x, y, color):
        if (x, y) in self.cells:
            self.canvas.itemconfig(self.cells[(x, y)], fill=color)
    
    def on_click(self, event):
        if not self.drag_complete:
            x, y = self.get_cell_coords(event)
            if 0 <= x < self.x_size and 0 <= y < self.y_size:
                self.start_x = x
                self.start_y = y
                self.dragging = True
                self.highlight_cell(x, y, 'lightblue')
        else:
            # This is the final click after drag is complete
            x, y = self.get_cell_coords(event)
            if 0 <= x < self.x_size and 0 <= y < self.y_size:
                self.final_x = x
                self.final_y = y
                self.highlight_cell(x, y, 'lightgreen')
                # Store result and close window
                self.result = (self.start_x, self.start_y, 
                             self.end_x, self.end_y,
                             self.final_x, self.final_y)
                self.root.quit()
    
    def on_drag(self, event):
        if self.dragging:
            x, y = self.get_cell_coords(event)
            if 0 <= x < self.x_size and 0 <= y < self.y_size:
                self.end_x = x
                self.end_y = y
                # Reset all cells except start
                for cell_x in range(self.x_size):
                    for cell_y in range(self.y_size):
                        if (cell_x, cell_y) != (self.start_x, self.start_y):
                            self.highlight_cell(cell_x, cell_y, 'white')
                # Highlight current cell
                self.highlight_cell(x, y, 'yellow')
    
    def on_release(self, event):
        if self.dragging:
            x, y = self.get_cell_coords(event)
            if 0 <= x < self.x_size and 0 <= y < self.y_size:
                self.end_x = x
                self.end_y = y
                self.highlight_cell(x, y, 'pink')
                self.dragging = False
                self.drag_complete = True
    
    def get_move(self):
        self.root.mainloop()
        return self.result

def get_user_move(x_size=8, y_size=8):
    """
    Create the GUI and get the user's move.
    Returns tuple (x1, y1, x2, y2, x3, y3) representing:
    - Starting position (x1, y1)
    - Ending position of drag (x2, y2)
    - Final clicked position (x3, y3)
    """
    gui = GameBoardGUI(x_size, y_size)
    return gui.get_move()

# Example usage:
# if __name__ == "__main__":
#     move = get_user_move()
#     print(f"Move coordinates: {move}")