using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WindowsPreview.Kinect;
using Microsoft.Kinect.Face;
using Windows.UI.Xaml.Controls;
using Windows.UI;
using Windows.Foundation;
using System.Diagnostics;
using Windows.UI.Xaml.Shapes;
using Windows.UI.Xaml.Media;

namespace KinectFace
{
    public class FaceManager
    {
        /// <summary>
        /// Face rotation display angle increment in degrees
        /// </summary>
        private const double FaceRotationIncrementInDegrees = 5.0;

        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor kinectSensor = null;

        /// <summary>
        /// Reader for body frames
        /// </summary>
        private BodyFrameReader bodyFrameReader = null;

        /// <summary>
        /// Array to store bodies
        /// </summary>
        private Body[] bodies = null;

        /// <summary>
        /// Number of bodies tracked
        /// </summary>
        private int bodyCount;

        /// <summary>
        /// Face frame sources
        /// </summary>
        private FaceFrameSource[] faceFrameSources = null;

        /// <summary>
        /// Face frame readers
        /// </summary>
        private FaceFrameReader[] faceFrameReaders = null;

        /// <summary>
        /// List of colors for each face tracked
        /// </summary>
        private List<Color> faceColors;

        /// <summary>
        /// Create a new FaceManager using an existing sensor which has already opened.
        /// The faceFrameResult will return the requested faceFeatures.
        /// </summary>
        /// <param name="sensor"></param>
        /// <param name="faceFeatures"></param>
        public FaceManager(KinectSensor sensor, FaceFrameFeatures faceFeatures)
        {
            this.kinectSensor = sensor;

            // open the reader for the body frames
            this.bodyFrameReader = this.kinectSensor.BodyFrameSource.OpenReader();

            // wire handler for body frame arrival
            this.bodyFrameReader.FrameArrived += this.Reader_BodyFrameArrived;

            this.bodyCount = this.kinectSensor.BodyFrameSource.BodyCount;

            // allocate storage to store body objects
            this.bodies = new Body[this.bodyCount];

            // create a face frame source + reader to track each face in the FOV
            this.faceFrameSources = new FaceFrameSource[this.bodyCount];
            this.faceFrameReaders = new FaceFrameReader[this.bodyCount];

            for (int i = 0; i < this.bodyCount; i++)
            {
                // create the face frame source with the required face frame features and an initial tracking Id of 0
                this.faceFrameSources[i] = new FaceFrameSource(this.kinectSensor, 0, faceFeatures);

                // open the corresponding reader
                this.faceFrameReaders[i] = this.faceFrameSources[i].OpenReader();
            }

            // populate face result colors - one for each face index
            this.faceColors = new List<Color>()
            {
                Colors.Red,
                Colors.Orange,
                Colors.Green,
                Colors.LightBlue,
                Colors.Indigo,
                Colors.Violet
            };
        }

        /// <summary>
        /// Stop all the frame readers required for face tracking.
        /// </summary>
        /// <returns></returns>
        public bool StopFrameReaders()
        {
            for (int i = 0; i < this.bodyCount; i++)
            {
                if (this.faceFrameReaders[i] != null)
                {
                    // FaceFrameReader is IDisposable
                    this.faceFrameReaders[i].Dispose();
                    this.faceFrameReaders[i] = null;
                }

                if (this.faceFrameSources[i] != null)
                {
                    this.faceFrameSources[i] = null;
                }
            }

            if (this.bodyFrameReader != null)
            {
                // BodyFrameReader is IDisposable
                this.bodyFrameReader.Dispose();
                this.bodyFrameReader = null;
            }
            return false;
        }

        /// <summary>
        /// Responding to the BodyFrameArrived event. This maps the faceFrameSources 
        /// tracking id's to those of the tracked bodies.
        /// </summary>
        private void Reader_BodyFrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            using (var bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    // update body data
                    bodyFrame.GetAndRefreshBodyData(this.bodies);

                    // iterate through each face source
                    for (int i = 0; i < this.bodyCount; i++)
                    {
                        // check if a valid face is tracked in this face source
                        if (!this.faceFrameSources[i].IsTrackingIdValid)
                        {
                            // check if the corresponding body is tracked 
                            if (this.bodies[i].IsTracked)
                            {
                                // update the face frame source to track this body
                                this.faceFrameSources[i].TrackingId = this.bodies[i].TrackingId;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Get the latest FaceFrameResult from a single bodyIndex. If there is a valid body tracked
        /// at that index then the FaceFrameResult will be valid.
        /// </summary>
        /// <param name="bodyId">The index of the tracked body</param>
        /// <returns>A FaceFrameResult from the matching FaceFrameReader</returns>
        public FaceFrameResult GetLatestFaceFrameResult(int bodyId)
        {
            if (this.faceFrameReaders[bodyId] != null)
            {
                return this.faceFrameReaders[bodyId].AcquireLatestFrame().FaceFrameResult;
            }
            return null;
        }

        /// <summary>
        /// Acquire the latest FaceFrameResults from all the FaceFrameReaders. 
        /// They must be checked for validity.
        /// </summary>
        /// <returns>Array of FaceFrames of bodyCount size (6 with KinectV2)</returns>
        public FaceFrameResult[] GetLatestFaceFrameResults()
        {
            FaceFrameResult[] results = new FaceFrameResult[bodyCount];
            for (int i = 0; i < this.bodyCount; i++)
            {
                if (this.faceFrameReaders[i] != null)
                {
                    // wire handler for face frame arrival
                    FaceFrame frame= this.faceFrameReaders[i].AcquireLatestFrame();
                    if (frame != null && frame.FaceFrameResult != null)
                    {
                        results[i] = frame.FaceFrameResult;
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Acquire the latest valid face frames and draw their data to a panel with 
        /// WPF shapes and text.
        /// </summary>
        /// <param name="parentPanel">The Panel in which the shapes will be children. 
        /// Make sure the parentPanel has its width and height set to what is expected 
        /// for the requested features.</param>
        /// <param name="displayFeatures">The FaceFrameFeatures to be rendered, use flags to 
        /// display many features. Be aware of ColorSpace vs InfraredSpace when choosing.</param>
        public void DrawLatestFaceResults(Panel parentPanel, FaceFrameFeatures displayFeatures)
        {
            parentPanel.Children.Clear();
            for (int i = 0; i < this.bodyCount; i++)
            {
                if (this.faceFrameReaders[i] != null)
                {
                    FaceFrame frame = this.faceFrameReaders[i].AcquireLatestFrame();
                    if (frame != null && frame.FaceFrameResult != null)
                    {
                        DrawFaceFeatures(parentPanel, frame.FaceFrameResult, displayFeatures, this.faceColors[i], i);
                    }
                }
            }
        }

        /// <summary>
        /// Create shapes and text to apply to a parent panel which represent the
        /// current state of a faceFrameResult.
        /// </summary>
        /// <param name="parentPanel">The Panel in which the shapes will be added</param>
        /// <param name="faceFrameResult">The reult of the face tracking</param>
        /// <param name="displayFeatures">The FaceFrameFeatures to be drawn</param>
        /// <param name="color">The color of the shapes and text</param>
        /// <param name="bodyIndex">The index of the body/face</param>
        private void DrawFaceFeatures(Panel parentPanel, FaceFrameResult faceFrameResult, FaceFrameFeatures displayFeatures, Color color, int bodyIndex)
        {
            if (parentPanel.Width == 0 ||
                Double.IsNaN(parentPanel.Width) ||
                parentPanel.Height == 0 ||
                Double.IsNaN(parentPanel.Height))
            {
                // The parent Panel must have a size to be rendered on
                return;
            }

            string messages = "";
            bool renderMessages = false;
            int fontSize = (int)((double)parentPanel.Height * (double)0.023);
            Point messagesPosition = new Point(fontSize * bodyIndex * 10, 0);


            // Face points and bounding boxes
            if (displayFeatures.HasFlag(FaceFrameFeatures.BoundingBoxInColorSpace))
            {
                double lineSize = 7;
                int posX = faceFrameResult.FaceBoundingBoxInColorSpace.Left;
                int posY = faceFrameResult.FaceBoundingBoxInColorSpace.Top;
                int width = faceFrameResult.FaceBoundingBoxInColorSpace.Right - posX + (int)lineSize;
                int height = faceFrameResult.FaceBoundingBoxInColorSpace.Bottom - posY + (int)lineSize;
                Rectangle rect = CreateFaceBoxRectangle(color, lineSize, width, height);
                Canvas.SetLeft(rect, posX);
                Canvas.SetTop(rect, posY);
                parentPanel.Children.Add(rect);
                messagesPosition = new Point(posX, posY + height);
            }
            if (displayFeatures.HasFlag(FaceFrameFeatures.BoundingBoxInInfraredSpace))
            {
                double lineSize = 7;
                int posX = faceFrameResult.FaceBoundingBoxInInfraredSpace.Left;
                int posY = faceFrameResult.FaceBoundingBoxInInfraredSpace.Top;
                int width = faceFrameResult.FaceBoundingBoxInInfraredSpace.Right - posX + (int)lineSize;
                int height = faceFrameResult.FaceBoundingBoxInInfraredSpace.Bottom - posY + (int)lineSize;
                Rectangle rect = CreateFaceBoxRectangle(color, lineSize, width, height);
                Canvas.SetLeft(rect, posX);
                Canvas.SetTop(rect, posY);
                parentPanel.Children.Add(rect);
                messagesPosition = new Point(posX, posY + height);
            }
            if (displayFeatures.HasFlag(FaceFrameFeatures.PointsInColorSpace))
            {
                foreach (KeyValuePair<FacePointType, Point> facePointKVP in
                        faceFrameResult.FacePointsInColorSpace)
                {
                    Size ellipseSize = new Size(10, 10);
                    Ellipse ellipse = CreateFacePointEllipse(color, ellipseSize);
                    Canvas.SetLeft(ellipse, facePointKVP.Value.X - (ellipseSize.Width / 2));
                    Canvas.SetTop(ellipse, facePointKVP.Value.Y - (ellipseSize.Height / 2));
                    parentPanel.Children.Add(ellipse);
                }
            }
            if (displayFeatures.HasFlag(FaceFrameFeatures.PointsInInfraredSpace))
            {
                foreach (KeyValuePair<FacePointType, Point> facePointKVP in
                        faceFrameResult.FacePointsInInfraredSpace)
                {
                    Size ellipseSize = new Size(3, 3);
                    Ellipse ellipse = CreateFacePointEllipse(color, ellipseSize);
                    Canvas.SetLeft(ellipse, facePointKVP.Value.X - (ellipseSize.Width / 2));
                    Canvas.SetTop(ellipse, facePointKVP.Value.Y - (ellipseSize.Height / 2));
                    parentPanel.Children.Add(ellipse);
                }
            }
            // Rotation stuff
            if (displayFeatures.HasFlag(FaceFrameFeatures.RotationOrientation))
            {
                int pitch, yaw, roll = 0;
                ExtractFaceRotationInDegrees(faceFrameResult.FaceRotationQuaternion,
                    out pitch, out yaw, out roll);
                messages += "Rotation Pitch: " + pitch + "\n";
                messages += "Rotation Yaw: " + yaw + "\n";
                messages += "Rotation Roll: " + roll + "\n";
                renderMessages = true;
            }

            // Other Face Properties and states
            if (displayFeatures.HasFlag(FaceFrameFeatures.FaceEngagement))
            {
                messages += FacePropertyToString(FaceProperty.Engaged,
                    faceFrameResult.FaceProperties[FaceProperty.Engaged]);
                renderMessages = true;
            }
            if (displayFeatures.HasFlag(FaceFrameFeatures.Glasses))
            {
                messages += FacePropertyToString(FaceProperty.WearingGlasses,
                    faceFrameResult.FaceProperties[FaceProperty.WearingGlasses]);
                renderMessages = true;
            }
            if (displayFeatures.HasFlag(FaceFrameFeatures.Happy))
            {
                messages += FacePropertyToString(FaceProperty.Happy,
                    faceFrameResult.FaceProperties[FaceProperty.Happy]);
                renderMessages = true;
            }
            if (displayFeatures.HasFlag(FaceFrameFeatures.LeftEyeClosed))
            {
                messages += FacePropertyToString(FaceProperty.LeftEyeClosed,
                    faceFrameResult.FaceProperties[FaceProperty.LeftEyeClosed]);
                renderMessages = true;
            }
            if (displayFeatures.HasFlag(FaceFrameFeatures.RightEyeClosed))
            {
                messages += FacePropertyToString(FaceProperty.RightEyeClosed,
                    faceFrameResult.FaceProperties[FaceProperty.RightEyeClosed]);
                renderMessages = true;
            }
            if (displayFeatures.HasFlag(FaceFrameFeatures.LookingAway))
            {
                messages += FacePropertyToString(FaceProperty.LookingAway,
                    faceFrameResult.FaceProperties[FaceProperty.LookingAway]);
                renderMessages = true;
            }
            if (displayFeatures.HasFlag(FaceFrameFeatures.MouthMoved))
            {
                messages += FacePropertyToString(FaceProperty.MouthMoved,
                    faceFrameResult.FaceProperties[FaceProperty.MouthMoved]);
                renderMessages = true;
            }
            if (displayFeatures.HasFlag(FaceFrameFeatures.MouthOpen))
            {
                messages += FacePropertyToString(FaceProperty.MouthOpen,
                    faceFrameResult.FaceProperties[FaceProperty.MouthOpen]);
                renderMessages = true;
            }

            if (renderMessages)
            {
                TextBlock textBlock = new TextBlock();
                textBlock.Text = messages;
                textBlock.Foreground = new SolidColorBrush(color);
                textBlock.FontSize = fontSize;
                Canvas.SetLeft(textBlock, messagesPosition.X);
                Canvas.SetTop(textBlock, messagesPosition.Y);
                parentPanel.Children.Add(textBlock);
            }
        }

        /// <summary>
        /// Create a Rectangle to display
        /// </summary>
        /// <param name="color">The Color of the stroke</param>
        /// <param name="strokeThickness">The thickness of the stroke</param>
        /// <param name="width">The width of the Rectangle</param>
        /// <param name="height">The height of the Rectangle</param>
        /// <returns></returns>
        private Rectangle CreateFaceBoxRectangle(Color color, double strokeThickness, double width, double height)
        {
            Rectangle rect = new Rectangle();
            rect.Width = width;
            rect.Height = height;
            rect.Stroke = new SolidColorBrush(color);
            rect.StrokeThickness = strokeThickness;
            rect.StrokeLineJoin = PenLineJoin.Round;
            rect.Fill = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
            return rect;
        }

        /// <summary>
        /// Create an Ellipse to display
        /// </summary>
        /// <param name="color">The Color of the Ellipse</param>
        /// <param name="size">The Size of the Ellipse</param>
        /// <returns></returns>
        private Ellipse CreateFacePointEllipse(Color color, Size size)
        {
            Ellipse baseEllipse = new Ellipse();
            baseEllipse.Width = size.Width;
            baseEllipse.Height = size.Height;
            baseEllipse.Fill = new SolidColorBrush(color);
            return baseEllipse;
        }

        /// <summary>
        /// Convert the FaceProperty and DetectionResult to a display string   
        /// </summary>
        /// <param name="prop">The FaceProperty enum type</param>
        /// <param name="result">The DetectionResult of the FaceProperty</param>
        /// <returns></returns>
        private string FacePropertyToString(FaceProperty prop, DetectionResult result)
        {
            string str = "";
            str += Enum.GetName(typeof(FaceProperty), prop) + ": ";
            str += Enum.GetName(typeof(DetectionResult), result) + "\n";
            return str;
        }


        /// <summary>
        /// Converts rotation quaternion to Euler angles 
        /// And then maps them to a specified range of values to control the refresh rate
        /// </summary>
        /// <param name="rotQuaternion">face rotation quaternion</param>
        /// <param name="pitch">rotation about the X-axis</param>
        /// <param name="yaw">rotation about the Y-axis</param>
        /// <param name="roll">rotation about the Z-axis</param>
        private static void ExtractFaceRotationInDegrees(Vector4 rotQuaternion, out int pitch, out int yaw, out int roll)
        {
            double x = rotQuaternion.X;
            double y = rotQuaternion.Y;
            double z = rotQuaternion.Z;
            double w = rotQuaternion.W;

            // convert face rotation quaternion to Euler angles in degrees
            double yawD, pitchD, rollD;
            pitchD = Math.Atan2(2 * ((y * z) + (w * x)), (w * w) - (x * x) - (y * y) + (z * z)) / Math.PI * 180.0;
            yawD = Math.Asin(2 * ((w * y) - (x * z))) / Math.PI * 180.0;
            rollD = Math.Atan2(2 * ((x * y) + (w * z)), (w * w) + (x * x) - (y * y) - (z * z)) / Math.PI * 180.0;

            // clamp the values to a multiple of the specified increment to control the refresh rate
            double increment = FaceRotationIncrementInDegrees;
            pitch = (int)(Math.Floor((pitchD + ((increment / 2.0) * (pitchD > 0 ? 1.0 : -1.0))) / increment) * increment);
            yaw = (int)(Math.Floor((yawD + ((increment / 2.0) * (yawD > 0 ? 1.0 : -1.0))) / increment) * increment);
            roll = (int)(Math.Floor((rollD + ((increment / 2.0) * (rollD > 0 ? 1.0 : -1.0))) / increment) * increment);
        }

    }
}
