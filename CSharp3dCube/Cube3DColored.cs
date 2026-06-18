using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace test_ui;

public class Cube3DColored : Form
{
    private System.Windows.Forms.Timer timer;  // Fixed: Full namespace
    private float rotationX = 0f;
    private float rotationY = 0f;
    private float rotationZ = 0f;
    private bool autoRotate = true;
    private Point lastMousePos;
    
    // Cube face definitions (4 vertices per face)
    private Face[] faces = new Face[]
    {
        new Face(new int[] {0,1,2,3}, Color.FromArgb(200, 255, 0, 0)),   // Back - Red
        new Face(new int[] {4,5,6,7}, Color.FromArgb(200, 0, 255, 0)),   // Front - Green
        new Face(new int[] {0,1,5,4}, Color.FromArgb(200, 0, 0, 255)),   // Bottom - Blue
        new Face(new int[] {2,3,7,6}, Color.FromArgb(200, 255, 255, 0)), // Top - Yellow
        new Face(new int[] {0,3,7,4}, Color.FromArgb(200, 255, 0, 255)), // Left - Magenta
        new Face(new int[] {1,2,6,5}, Color.FromArgb(200, 0, 255, 255))  // Right - Cyan
    };
    
    private Point3D[] cubePoints = new Point3D[]
    {
        new Point3D(-1, -1, -1), // 0
        new Point3D( 1, -1, -1), // 1
        new Point3D( 1,  1, -1), // 2
        new Point3D(-1,  1, -1), // 3
        new Point3D(-1, -1,  1), // 4
        new Point3D( 1, -1,  1), // 5
        new Point3D( 1,  1,  1), // 6
        new Point3D(-1,  1,  1)  // 7
    };
    
    public Cube3DColored()
    {
        this.Text = "3D Cube - Colored";
        this.Size = new Size(600, 600);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.BackColor = Color.FromArgb(30, 30, 40);
        this.DoubleBuffered = true;
        
        // Timer for animation
        timer = new System.Windows.Forms.Timer();  // Fixed: Full namespace
        timer.Interval = 16;
        timer.Tick += (s, e) => 
        {
            if (autoRotate)
            {
                rotationX += 0.01f;
                rotationY += 0.015f;
                rotationZ += 0.005f;
            }
            this.Invalidate();
        };
        timer.Start();
        
        // Mouse events for manual rotation
        this.MouseDown += (s, e) => { 
            lastMousePos = e.Location; 
            autoRotate = false; 
        };
        this.MouseMove += (s, e) => {
            if (e.Button == MouseButtons.Left)
            {
                float dx = e.X - lastMousePos.X;
                float dy = e.Y - lastMousePos.Y;
                rotationY += dx * 0.01f;
                rotationX += dy * 0.01f;
                lastMousePos = e.Location;
                this.Invalidate();
            }
        };
        this.MouseUp += (s, e) => { autoRotate = true; };
        
        // Keyboard controls
        this.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.R) // Reset rotation
            {
                rotationX = rotationY = rotationZ = 0;
            }
            if (e.KeyCode == Keys.Space) // Toggle auto-rotation
            {
                autoRotate = !autoRotate;
            }
        };
        this.KeyPreview = true;
        
        this.Paint += Cube3DColored_Paint!;  // Fixed: Added null-forgiving operator
    }
    
    private void Cube3DColored_Paint(object? sender, PaintEventArgs e)  // Fixed: Added '?' for nullable
    {
        Graphics graphics = e.Graphics;  // Fixed: Renamed to avoid conflict
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        
        int centerX = this.ClientSize.Width / 2;
        int centerY = this.ClientSize.Height / 2;
        float scale = 150f;
        
        // Get all rotated points
        Point3D[] rotated = new Point3D[8];
        for (int i = 0; i < 8; i++)
        {
            rotated[i] = RotatePoint(cubePoints[i]);
        }
        
        // Project to 2D with perspective
        PointF[] projected = new PointF[8];
        float fov = 4f;
        float zOffset = 3f;
        for (int i = 0; i < 8; i++)
        {
            float perspective = fov / (zOffset + rotated[i].Z);
            projected[i] = new PointF(
                centerX + rotated[i].X * scale * perspective,
                centerY - rotated[i].Y * scale * perspective
            );
        }
        
        // Calculate face centers (for sorting)
        FaceData[] faceData = new FaceData[faces.Length];
        for (int f = 0; f < faces.Length; f++)
        {
            // Calculate average Z of face vertices
            float avgZ = 0;
            foreach (int idx in faces[f].VertexIndices)
            {
                avgZ += rotated[idx].Z;
            }
            avgZ /= faces[f].VertexIndices.Length;
            
            faceData[f] = new FaceData
            {
                Face = faces[f],
                AvgZ = avgZ,
                ProjectedPoints = Array.ConvertAll(
                    faces[f].VertexIndices, 
                    idx => projected[idx]
                )
            };
        }
        
        // Sort faces by depth (back to front)
        Array.Sort(faceData, (a, b) => a.AvgZ.CompareTo(b.AvgZ));
        
        // Draw each face
        foreach (var fd in faceData)
        {
            // Create path for the face
            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddPolygon(fd.ProjectedPoints);
                
                // Calculate lighting based on face normal
                float brightness = CalculateBrightness(fd.Face, rotated);
                
                // Use semi-transparent color with brightness
                Color baseColor = fd.Face.Color;
                int r = (int)(baseColor.R * brightness);
                int g = (int)(baseColor.G * brightness);
                int b = (int)(baseColor.B * brightness);
                Color fillColor = Color.FromArgb(baseColor.A, 
                    Math.Min(r, 255), Math.Min(g, 255), Math.Min(b, 255));
                
                using (SolidBrush brush = new SolidBrush(fillColor))
                using (Pen outlinePen = new Pen(Color.White, 1.5f))
                {
                    graphics.FillPath(brush, path);  // Fixed: Using graphics variable
                    graphics.DrawPath(outlinePen, path);  // Fixed: Using graphics variable
                }
            }
        }
        
        // Draw controls info
        using (Brush brush = new SolidBrush(Color.White))
        using (Font font = new Font("Arial", 10))
        {
            string info = $"Drag: Rotate | Space: Toggle Auto ({ (autoRotate ? "ON" : "OFF") }) | R: Reset";
            graphics.DrawString(info, font, brush, 10, this.ClientSize.Height - 30);  // Fixed: Using graphics variable
        }
    }
    
    private float CalculateBrightness(Face face, Point3D[] rotatedPoints)
    {
        // Get three points to calculate normal
        int[] idx = face.VertexIndices;
        Point3D p0 = rotatedPoints[idx[0]];
        Point3D p1 = rotatedPoints[idx[1]];
        Point3D p2 = rotatedPoints[idx[2]];
        
        // Calculate two edge vectors
        Point3D v1 = new Point3D(p1.X - p0.X, p1.Y - p0.Y, p1.Z - p0.Z);
        Point3D v2 = new Point3D(p2.X - p0.X, p2.Y - p0.Y, p2.Z - p0.Z);
        
        // Calculate normal (cross product)
        Point3D normal = new Point3D(
            v1.Y * v2.Z - v1.Z * v2.Y,
            v1.Z * v2.X - v1.X * v2.Z,
            v1.X * v2.Y - v1.Y * v2.X
        );
        
        // Normalize normal
        float length = (float)Math.Sqrt(normal.X * normal.X + normal.Y * normal.Y + normal.Z * normal.Z);
        if (length > 0)
        {
            normal.X /= length;
            normal.Y /= length;
            normal.Z /= length;
        }
        
        // Light direction (from front-top-right)
        Point3D lightDir = new Point3D(0.5f, 0.8f, 0.6f);
        float lightLength = (float)Math.Sqrt(lightDir.X * lightDir.X + lightDir.Y * lightDir.Y + lightDir.Z * lightDir.Z);
        lightDir.X /= lightLength;
        lightDir.Y /= lightLength;
        lightDir.Z /= lightLength;
        
        // Dot product for brightness
        float brightness = normal.X * lightDir.X + normal.Y * lightDir.Y + normal.Z * lightDir.Z;
        brightness = Math.Max(0.3f, Math.Min(1f, brightness + 0.3f)); // Clamp with ambient light
        
        return brightness;
    }
    
    private Point3D RotatePoint(Point3D p)
    {
        // X-axis rotation
        float cosX = (float)Math.Cos(rotationX);
        float sinX = (float)Math.Sin(rotationX);
        float y1 = p.Y * cosX - p.Z * sinX;
        float z1 = p.Y * sinX + p.Z * cosX;
        
        // Y-axis rotation
        float cosY = (float)Math.Cos(rotationY);
        float sinY = (float)Math.Sin(rotationY);
        float x2 = p.X * cosY + z1 * sinY;
        float z2 = -p.X * sinY + z1 * cosY;  // z2 is created here
        
        // Z-axis rotation
        float cosZ = (float)Math.Cos(rotationZ);
        float sinZ = (float)Math.Sin(rotationZ);
        float x3 = x2 * cosZ - y1 * sinZ;
        float y3 = x2 * sinZ + y1 * cosZ;
        
        return new Point3D(x3, y3, z2);  // Fixed: Using z2 instead of z3
    }
    
    private struct Point3D
    {
        public float X, Y, Z;
        public Point3D(float x, float y, float z)
        {
            X = x; Y = y; Z = z;
        }
    }
    
    private struct Face
    {
        public int[] VertexIndices;
        public Color Color;
        public Face(int[] indices, Color color)
        {
            VertexIndices = indices;
            Color = color;
        }
    }
    
    private struct FaceData
    {
        public Face Face;
        public float AvgZ;
        public PointF[] ProjectedPoints;
    }
}