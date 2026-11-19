using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace SolidModelBrowser
{
    public partial class Scene : UserControl
    {
        GeometryModel3D geometry = new GeometryModel3D();
        MeshGeometry3D mesh = new MeshGeometry3D();
        GeometryModel3D wireframeGeometry = new GeometryModel3D();
        MeshGeometry3D wireframeMesh = new MeshGeometry3D();
        MaterialGroup wireframeMaterial = new MaterialGroup();
        DiffuseMaterial wireframeDiffuse = new DiffuseMaterial();
        EmissiveMaterial wireframeEmissive = new EmissiveMaterial();
        UCamera camera;

        public Scene()
        {
            InitializeComponent();
            geometry.Geometry = mesh;
            geometry.Material = materials;
            wireframeMaterial.Children.Add(wireframeDiffuse);
            wireframeMaterial.Children.Add(wireframeEmissive);
            wireframeGeometry.Geometry = wireframeMesh;
            wireframeGeometry.Material = wireframeMaterial;
            wireframeGeometry.BackMaterial = wireframeMaterial;
            UpdateWireframeColor(false, false, Color.FromRgb(154, 162, 170));

            modelgroup.Children.Add(geometry);
            modelgroup.Children.Add(lights);

            camera = new UCamera(viewport3D);
        }

        
        // mouse controllers

        Point mLastPos;

        private void Viewport_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            camera.Scale(e.Delta > 0 ? 0.9 : 1.1);
        }

        private void Viewport_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed || e.RightButton == MouseButtonState.Pressed || e.MiddleButton == MouseButtonState.Pressed)
            {
                Point pos = Mouse.GetPosition(viewport);
                Point actualPos = new Point(pos.X - viewport.ActualWidth / 2, viewport.ActualHeight / 2 - pos.Y);
                double dx = actualPos.X - mLastPos.X, dy = actualPos.Y - mLastPos.Y;

                if (e.LeftButton == MouseButtonState.Pressed) camera.Move(dx, dy);
                if (e.RightButton == MouseButtonState.Pressed) camera.Orbit(dx, dy);

                mLastPos = actualPos;
            }
        }

        private void Viewport_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && Keyboard.IsKeyDown(Key.LeftShift)) { ShiftAndDrag?.Invoke(this, EventArgs.Empty); return; }

            Point pos = Mouse.GetPosition(viewport);
            mLastPos = new Point(pos.X - viewport.ActualWidth / 2, viewport.ActualHeight / 2 - pos.Y);
            ((IInputElement)sender).CaptureMouse();
        }

        private void Viewport_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ((IInputElement)sender).ReleaseMouseCapture();
        }

        // materials

        DiffuseMaterial matDiffuse;
        SpecularMaterial matSpecular;
        EmissiveMaterial matEmissive;
        DiffuseMaterial matBackDiffuse;
        
        public void CreateMaterials(Color diffuse, Color specular, Color emissive, Color backDiffuse, double specularPower)
        {
            matDiffuse = new DiffuseMaterial(new SolidColorBrush(diffuse));
            //matDiffuse.AmbientColor = Color.FromArgb(255, 0, 255, 0);
            matSpecular = new SpecularMaterial(new SolidColorBrush(specular), specularPower);
            matEmissive = new EmissiveMaterial(new SolidColorBrush(emissive));
            matBackDiffuse = new DiffuseMaterial(new SolidColorBrush(backDiffuse));
        }

        MaterialGroup materials = new MaterialGroup();

        public void ApplyMaterials(bool diffuse, bool specular, bool emissive, bool backDiffuse)
        {
            materials.Children.Clear();
            if (diffuse) materials.Children.Add(matDiffuse);
            if (specular) materials.Children.Add(matSpecular);
            if (emissive) materials.Children.Add(matEmissive);
            geometry.Material = materials;

            geometry.BackMaterial = backDiffuse ? matBackDiffuse : null;
        }

        // lights

        Model3DGroup lights = new Model3DGroup();

        public void RemoveAllLights() => lights.Children.Clear();
        public void CreateAmbientLight(Color c) => lights.Children.Add(new AmbientLight(c));
        public void CreateDirectionalLight(Color c, Vector3D dir) => lights.Children.Add(new DirectionalLight(c, dir));

        // axes

        enum Axis { X, Y, Z }
        GeometryModel3D createAxis(Axis axis, double len, Color color)
        {
            Point3DCollection p = null;
            if (axis == Axis.X) p = new Point3DCollection() { new Point3D(0, 0, 0), new Point3D(len, 0, 0), new Point3D(0, 0, 1), new Point3D(len, 0, 1) };
            if (axis == Axis.Y) p = new Point3DCollection() { new Point3D(0, 0, 0), new Point3D(0, len, 0), new Point3D(0, 0, 1), new Point3D(0, len, 1) };
            if (axis == Axis.Z) p = new Point3DCollection() { new Point3D(0, 0, 0), new Point3D(0, 0, len), new Point3D(1, 0, 0), new Point3D(1, 0, len) };
            var i = new Int32Collection() { 0, 1, 2, 1, 3, 2 };
            var mesh = new MeshGeometry3D() { Positions = p, TriangleIndices = i };
            var mat = new DiffuseMaterial(new SolidColorBrush(color));
            return new GeometryModel3D() { Geometry = mesh, Material = mat, BackMaterial = mat };
        }

        Model3DGroup axes = new Model3DGroup();
        public void CreateAxes(double len)
        {
            axes.Children.Clear();
            axes.Children.Add(createAxis(Axis.X, len, Color.FromArgb(160, 255, 0, 0)));
            axes.Children.Add(createAxis(Axis.Y, len, Color.FromArgb(160, 0, 255, 0)));
            axes.Children.Add(createAxis(Axis.Z, len, Color.FromArgb(160, 0, 0, 255)));
        }

        // ground

        GeometryModel3D ground = new GeometryModel3D();
        Rect lastGroundRect;
        double lastCheckerSize;
        Color lastGroundDiffuse;
        Color lastGroundEmissive;
        double currentGroundZ = 0.0;

        public void CreateGround(Rect r, double checkerSize, Color diffuseColor, Color emissiveColor)
        {
            lastGroundRect = r;
            lastCheckerSize = checkerSize;
            lastGroundDiffuse = diffuseColor;
            lastGroundEmissive = emissiveColor;
            UpdateGroundMesh();
        }

        public void SetGroundZ(double z)
        {
            currentGroundZ = z;
            UpdateGroundMesh();
        }

        private void UpdateGroundMesh()
        {
            Rect r = lastGroundRect;
            double checkerSize = lastCheckerSize;
            if (r.Width == 0 || r.Height == 0 || checkerSize == 0) return;

            double z = currentGroundZ;
            var p = new Point3DCollection() {
                new Point3D(r.Left, r.Top, z),
                new Point3D(r.Right, r.Top, z),
                new Point3D(r.Left, r.Bottom, z),
                new Point3D(r.Right, r.Bottom, z)
            };
            var texcoord = new PointCollection() { new Point(0, 0), new Point(1, 0), new Point(0, 1), new Point(1, 1) };
            var i = new Int32Collection() { 0, 1, 2, 1, 3, 2 };
            var mesh = new MeshGeometry3D { Positions = p, TextureCoordinates = texcoord, TriangleIndices = i };

            var geomcoll = new GeometryCollection() { new RectangleGeometry(new Rect(0, 0, 50, 50)), new RectangleGeometry(new Rect(50, 50, 50, 50)) };
            var coloredDrawing = new GeometryDrawing() { Brush = new SolidColorBrush(lastGroundDiffuse), Geometry = new GeometryGroup() { Children = geomcoll } };
            var drawingBrush = new DrawingBrush { Viewport = new Rect(0, 0, 2 * checkerSize / r.Width, 2 * checkerSize / r.Height), TileMode = TileMode.Tile, Drawing = coloredDrawing };
            var diffuseMaterial = new DiffuseMaterial() { Brush = drawingBrush };
            var materialGroup = new MaterialGroup();
            materialGroup.Children.Add(diffuseMaterial);
            if (lastGroundEmissive.A > 0)
                materialGroup.Children.Add(new EmissiveMaterial() { Brush = new SolidColorBrush(lastGroundEmissive) });

            ground.Geometry = mesh;
            ground.Material = materialGroup;
            ground.BackMaterial = null;
        }


        // events and props

        public event EventHandler ShiftAndDrag;

        public MeshGeometry3D Mesh => mesh;
        public MeshGeometry3D WireframeMesh => wireframeMesh;
        public UCamera Camera => camera;
        public Viewport3D Viewport3D => viewport3D;

        public void UpdateWireframeColor(bool isPureWireframe, bool lightTheme, Color modelDiffuseColor)
        {
            Color color;
            if (isPureWireframe)
            {
                // Contrast with Viewport background
                color = lightTheme ? Color.FromRgb(20, 22, 26) : Color.FromRgb(225, 230, 238);
            }
            else
            {
                // Solid + Wireframe (CAD mode): Contrast with the model surface
                double lum = 0.299 * modelDiffuseColor.R + 0.587 * modelDiffuseColor.G + 0.114 * modelDiffuseColor.B;
                color = lum > 125 ? Color.FromRgb(18, 20, 24) : Color.FromRgb(242, 245, 250);
            }
            var brush = new SolidColorBrush(color);
            wireframeDiffuse.Brush = brush;
            wireframeEmissive.Brush = brush;
        }

        public void UpdateWireframeColor(bool lightTheme)
        {
            UpdateWireframeColor(false, lightTheme, lightTheme ? Color.FromRgb(220, 220, 220) : Color.FromRgb(32, 32, 38));
        }

        public void SetShadingVisibility(bool showSolid, bool showWireframe)
        {
            if (showSolid)
            {
                if (!modelgroup.Children.Contains(geometry)) modelgroup.Children.Add(geometry);
            }
            else
            {
                if (modelgroup.Children.Contains(geometry)) modelgroup.Children.Remove(geometry);
            }

            if (showWireframe)
            {
                if (modelgroup.Children.Contains(wireframeGeometry)) modelgroup.Children.Remove(wireframeGeometry);
                modelgroup.Children.Add(wireframeGeometry);
            }
            else
            {
                if (modelgroup.Children.Contains(wireframeGeometry)) modelgroup.Children.Remove(wireframeGeometry);
            }
        }

        bool isAxesVisible;
        public bool IsAxesVisible
        {
            get { return isAxesVisible; }
            set
            {
                isAxesVisible = value;
                if (modelgroup.Children.Contains(axes)) modelgroup.Children.Remove(axes);
                if (isAxesVisible) modelgroup.Children.Add(axes);
            }
        }

        bool isGroundVisible;
        public bool IsGroundVisible
        {
            get { return isGroundVisible; }
            set
            {
                isGroundVisible = value;
                if (modelgroup.Children.Contains(ground)) modelgroup.Children.Remove(ground);
                if (isGroundVisible)
                {
                    int wfIdx = modelgroup.Children.IndexOf(wireframeGeometry);
                    if (wfIdx >= 0) modelgroup.Children.Insert(wfIdx, ground);
                    else modelgroup.Children.Add(ground);
                }
            }
        }

    }
}
