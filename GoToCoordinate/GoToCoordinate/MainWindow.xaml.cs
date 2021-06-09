using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Threading;
using Transformer;
using TerraExplorerX;

namespace GoToCoordinate
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private int CSSelectedIndex;
        private Dictionary<int, int> CSLookUp; //key: index of combobox item, value: SRID
        private SGWorld71 sgworld;
        private string appdir;
        private Heading heading;
        private Coordinate mouse_coords;
        string SridFile;
        Thread mouse_position_thread;
        Thread shutdown_thread;
        Process[] tepname;

        public MainWindow()
        {
            InitializeComponent();
            CSSelectedIndex = comboBox.SelectedIndex;

            CSLookUp = new Dictionary<int, int>();
            CSLookUp.Add(0, 4326); // WGS84 Lat Lng
            CSLookUp.Add(1, 28349); // SRID of GDA94 MGA Zone 49
            CSLookUp.Add(2, 28350); // SRID of GDA94 MGA Zone 50
            CSLookUp.Add(3, 28351); // SRID of GDA94 MGA Zone 51
            CSLookUp.Add(4, 28352); // SRID of GDA94 MGA Zone 52
            CSLookUp.Add(5, 28353); // SRID of GDA94 MGA Zone 53
            CSLookUp.Add(6, 28354); // SRID of GDA94 MGA Zone 54
            CSLookUp.Add(7, 28355); // SRID of GDA94 MGA Zone 55
            CSLookUp.Add(8, 28356); // SRID of GDA94 MGA Zone 56
            CSLookUp.Add(9, 7844); // SRID of GDA94 MGA Zone 56
            CSLookUp.Add(10, 7850); // SRID of GDA94 MGA Zone 56

            sgworld = new SGWorld71();

            appdir = sgworld.Application.DataPath;
            SridFile = appdir + "\\Add-ons\\GoToCoordinate\\SRID.csv";

            heading = new Heading(sgworld);

            // create a new thread to run the mouse position update
            mouse_position_thread = new Thread(UpdateMousePositon);
            mouse_position_thread.Start();

            shutdown_thread = new Thread(ShutdownIfTerraNotRunning);
            shutdown_thread.Start();
        }

        private void UpdateMousePositon()
        {
            while(true)
            {
                try
                {

                    IMouseInfo71 mouse_info = sgworld.Window.GetMouseInfo();
                    int mouse_x = mouse_info.X;
                    int mouse_y = mouse_info.Y;

                    IWorldPointInfo71 wgs84_mouse_position = sgworld.Window.PixelToWorld(mouse_x, mouse_y, WorldPointType.WPT_TERRAIN);
                    double wgs84_mouse_x = wgs84_mouse_position.Position.X;
                    double wgs84_mouse_y = wgs84_mouse_position.Position.Y;

                    int fromCS = 4326; //wgs84 cs
                    int toCS = CSLookUp[CSSelectedIndex]; // the selected coordinate system

                    // if user has lat/lng selected
                    if (fromCS == toCS)
                    {
                        mouse_coords = new Coordinate(wgs84_mouse_y, wgs84_mouse_x);
                    }
                    else
                    {
                        TransformCoordinate transformer = new TransformCoordinate(SridFile, fromCS, toCS, wgs84_mouse_x, wgs84_mouse_y);
                        mouse_coords = new Coordinate(transformer.Transform()[1], transformer.Transform()[0]);
                    }

                    //Console.WriteLine(String.Format("{0},{1}", projected_coords[0], projected_coords[1]));

                    // not sure how this works but it fixes a problem with this thread accessing the main window thread:
                    // http://stackoverflow.com/questions/9732709/the-calling-thread-cannot-access-this-object-because-a-different-thread-owns-it
                    this.Dispatcher.Invoke(() =>
                    {
                        cursor_label.Content = String.Format("Cursor: {0:#.####} {1:#.####}", mouse_coords.X, mouse_coords.Y);
                    });

                    Thread.Sleep(15);
                }
                catch (Exception e)
                {
					//pass this try/catch is to handle shut downs of the tool
					Console.WriteLine(e);
                }
            }
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            int fromCS = CSLookUp[CSSelectedIndex]; // get SRID from CSLookUp dict based upon user combobox input
            int toCS = 4326; //WGS84
            double xcoord;
            double ycoord;
            string groupID = sgworld.ProjectTree.CreateGroup("Coordinate", "");

            try
            {
                xcoord = double.Parse(x_coord_text.Text);
                ycoord = double.Parse(y_coord_text.Text);
            }
            catch (Exception error)
            {
                Console.WriteLine(error);
                MessageBoxResult errorMessageBox = MessageBox.Show(this, String.Format("Coordinates are invalid:\n\nX: {0}\nY: {1}\n\nCoordinates need to be a floating point number.", x_coord_text.Text, y_coord_text.Text), "Invalid Coordinates", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            // transform the coordinate to WGS84
            TransformCoordinate transformer = new TransformCoordinate(SridFile, fromCS, toCS, xcoord, ycoord);
            double[] WGS84Coords = transformer.Transform();
            double x_wgs84 = WGS84Coords[0];
            double y_wgs84 = WGS84Coords[1];

            string markerName = String.Format("Coordinate: {0}, {1}", xcoord, ycoord);
            string markerFileLocation = appdir + "\\Add-ons\\GoToCoordinate\\marker.png";
            string kmlLocation = appdir + "\\Add-ons\\GoToCoordinate\\GoToCoordinate.kml";

            string KMLText = $@"<?xml version='1.0' encoding='UTF-8'?>
            <kml xmlns='http://www.opengis.net/kml/2.2' xmlns:gx='http://www.google.com/kml/ext/2.2' xmlns:kml='http://www.opengis.net/kml/2.2' xmlns:atom='http://www.w3.org/2005/Atom'>
            <Document>
	            <Style id='CoordinateMarker'>
		            <IconStyle>
			            <scale>1.0</scale>
			            <Icon>
				            <href>{markerFileLocation}</href>
			            </Icon>
			            <hotSpot x='0.5' y='1' xunits='fraction' yunits='fraction'/>
		            </IconStyle>
	            </Style>
	            <Placemark>
		            <name>{markerName}</name>
		            <styleUrl>#CoordinateMarker</styleUrl>
		            <Point>
			            <coordinates>{x_wgs84},{y_wgs84},0</coordinates>
		            </Point>
	            </Placemark>
            </Document>
            </kml>";

            File.WriteAllText(kmlLocation, KMLText);
            sgworld.Creator.CreateKMLLayer(kmlLocation, groupID);

            sgworld.ProjectTree.RenameGroup(groupID, string.Format("Coordinate: {0:f3}, {1:f3} (EPSG:{2})", xcoord, ycoord, fromCS));
            sgworld.ProjectTree.LockGroup(groupID, true);
        }

        private void find_heading(object sender, RoutedEventArgs e)
        {
            heading.EngageTool();
        }

        private void create_point(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("this is a test");
        }

        private void label_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CSSelectedIndex = comboBox.SelectedIndex;
        }

        private void Close_Button_Click(object sender, RoutedEventArgs e)
        {
            Shutdown();
        }

        private void Shutdown()
        {
            this.Dispatcher.Invoke(() =>
            {
                mouse_position_thread.Abort();
                Application.Current.Shutdown();
                shutdown_thread.Abort();
            });
        }

        private void ShutdownIfTerraNotRunning()
        {
            while (true)
            {
                tepname = Process.GetProcessesByName("TerraExplorer");
                if (tepname.Length == 0)
                {
                    Shutdown();
                }
            }
        }
    }
}
