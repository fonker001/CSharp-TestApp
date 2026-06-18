using System;
using System.Drawing;
using System.Windows.Forms;

namespace test_ui;

public class Cube3D : Form
{
    private System.Windows.Forms.Timer timer;  // Fixed: Full namespace
    private float rotationX = 0f;
    private float rotationY = 0f;
    private float rotationZ = 0f;
    
    // 3D points of a cube (centered at origin)
    private Point3D[] cubePoints = new Point3D[]
    {
        new Point3D(-1, -1, -1), // 0: back-bottom-left
        new Point3D( 1, -1, -1), // 1: back-bottom-right
        new Point3D( 1,  1, -1), // 2: back-top-right
        new Point3D(-1,  1, -1), // 3: back-top-left
        new Point3D(-1, -1,  1), // 4: front-bottom-left
        new Point3D( 1, -1,  1), // 5: front-bottom-right
        new Point3D( 1,  1,  1), // 6: front-top-right
        new Point3D(-1,  1,  1)  // 7: front-top-left
    };
    
    // Edges connecting the points
    private (int, int)[] edges = new (int, int)[]
    {
        (0,1), (1,2), (2,3), (3,0), // Back face
        (4,5), (5,6), (6,7), (7,4), // Front face
        (0,4), (1,5), (2,6), (3,7)  // Connecting edges
    };
    
    public Cube3D()
    {
        this.Text = "3D Cube - Simple";
        this.Size = new Size(500, 500);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.BackColor = Color.Black;
        this.DoubleBuffered = true; // Prevents flickering
        
        // Set up timer for animation
        timer = new System.Windows.Forms.Timer();  // Fixed: Full namespace
        timer.Interval = 16; // ~60 FPS
        timer.Tick += (s, e) => 
        {
            rotationX += 0.02f;
            rotationY += 0.03f;
            rotationZ += 0.01f;
            this.Invalidate(); // Trigger redraw
        };
        timer.Start();
        
        // Handle paint event
        this.Paint += Cube3D_Paint!;  // Fixed: Added null-forgiving operator
    }
    
    private void Cube3D_Paint(object? sender, PaintEventArgs e)  // Fixed: Added '?' for nullable
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        
        // Get center of form
        int centerX = this.ClientSize.Width / 2;
        int centerY = this.ClientSize.Height / 2;
        float scale = 150f; // Size of cube
        
        // Create pen for drawing
        using (Pen pen = new Pen(Color.Cyan, 2f))
        using (Pen backPen = new Pen(Color.DarkGray, 1f))
        {
            // Transform and draw each edge
            foreach (var edge in edges)
            {
                // Get rotated points
                PointF p1 = RotateAndProject(cubePoints[edge.Item1], centerX, centerY, scale);
                PointF p2 = RotateAndProject(cubePoints[edge.Item2], centerX, centerY, scale);
                
                // Calculate depth for shading (Z value after rotation)
                Point3D r1 = RotatePoint(cubePoints[edge.Item1]);
                Point3D r2 = RotatePoint(cubePoints[edge.Item2]);
                float avgZ = (r1.Z + r2.Z) / 2f;
                
                // Choose pen based on depth
                Pen usePen = avgZ > 0 ? pen : backPen;
                g.DrawLine(usePen, p1, p2);
            }
        }
        
        // Draw info text
        using (Brush brush = new SolidBrush(Color.White))
        using (Font font = new Font("Arial", 10))
        {
            g.DrawString($"Rotation: X={rotationX:F2}, Y={rotationY:F2}, Z={rotationZ:F2}", 
                font, brush, 10, 10);
        }
    }
    
    private PointF RotateAndProject(Point3D point, int centerX, int centerY, float scale)
    {
        // Apply rotation
        Point3D rotated = RotatePoint(point);
        
        // Simple perspective projection
        float fov = 4f; // Field of view
        float zOffset = 3f; // Distance from camera
        float perspective = fov / (zOffset + rotated.Z);
        
        // Project to 2D
        float x = rotated.X * scale * perspective;
        float y = rotated.Y * scale * perspective;
        
        return new PointF(centerX + x, centerY - y);
    }
    
    private Point3D RotatePoint(Point3D p)
    {
        // Rotate around X axis
        float cosX = (float)Math.Cos(rotationX);
        float sinX = (float)Math.Sin(rotationX);
        float y1 = p.Y * cosX - p.Z * sinX;
        float z1 = p.Y * sinX + p.Z * cosX;
        
        // Rotate around Y axis
        float cosY = (float)Math.Cos(rotationY);
        float sinY = (float)Math.Sin(rotationY);
        float x2 = p.X * cosY + z1 * sinY;
        float z2 = -p.X * sinY + z1 * cosY;  // Fixed: This was the issue
        
        // Rotate around Z axis
        float cosZ = (float)Math.Cos(rotationZ);
        float sinZ = (float)Math.Sin(rotationZ);
        float x3 = x2 * cosZ - y1 * sinZ;
        float y3 = x2 * sinZ + y1 * cosZ;
        
        return new Point3D(x3, y3, z2);  // Fixed: Using z2 instead of z3
    }
    
    // 3D Point structure
    private struct Point3D
    {
        public float X, Y, Z;
        public Point3D(float x, float y, float z)
        {
            X = x; Y = y; Z = z;
        }
    }
}