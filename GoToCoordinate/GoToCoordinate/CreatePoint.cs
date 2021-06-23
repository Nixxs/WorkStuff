using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerraExplorerX;

namespace GoToCoordinate
{
    class CreatePoint
    {
        private SGWorld71 _sgworld;
        private string appdir;
        private string groupID;
        private double lat1;
        private double lon1;
        private string markerImage;

        public CreatePoint(SGWorld71 sgworld)
        {
            appdir = _sgworld.Application.DataPath;
            _sgworld = sgworld;
            markerImage = appdir + "\\Add-ons\\GoToCoordinate\\marker.png";
        }

        public void EngageTool()
        {
            _sgworld.OnLButtonClicked += Sgworld_OnLButtonClicked;
            _sgworld.Window.SetInputMode(MouseInputMode.MI_COM_CLIENT);
            _sgworld.Window.ShowMessageBarText("Click on the terrain to create a point", MessageBarTextAlignment.MBT_CENTER, 7000);

            groupID = _sgworld.ProjectTree.CreateGroup("Point", "");
        }

        private bool Sgworld_OnLButtonClicked(int Flags, int X, int Y)
        {
            Console.WriteLine(string.Format("Flags:{0} X:{1} Y:{2}", Flags, X, Y));
            IWorldPointInfo71 wgs84_position = _sgworld.Window.PixelToWorld(X, Y, WorldPointType.WPT_TERRAIN);

            lat1 = wgs84_position.Position.Y;
            lon1 = wgs84_position.Position.X;

            string startMarkerName = "START";
            string startMarkerKmlLocation = appdir + "\\Add-ons\\GoToCoordinate\\BearingStart.kml";
            string startKMLText = $@"<?xml version='1.0' encoding='UTF-8'?>
                <kml xmlns='http://www.opengis.net/kml/2.2' xmlns:gx='http://www.google.com/kml/ext/2.2' xmlns:kml='http://www.opengis.net/kml/2.2' xmlns:atom='http://www.w3.org/2005/Atom'>
                <Document>
	                <Style id='CoordinateMarker'>
		                <IconStyle>
			                <scale>1.0</scale>
			                <Icon>
				                <href>{markerImage}</href>
			                </Icon>
			                <hotSpot x='0.5' y='1' xunits='fraction' yunits='fraction'/>
		                </IconStyle>
	                </Style>
	                <Placemark>
		                <name>{startMarkerName}</name>
		                <styleUrl>#CoordinateMarker</styleUrl>
		                <Point>
			                <coordinates>{lon1},{lat1},0</coordinates>
		                </Point>
	                </Placemark>
                </Document>
                </kml>";
            File.WriteAllText(startMarkerKmlLocation, startKMLText);

            // Add the KML and prevent the camera from moving
            IPosition71 cameraPosition = _sgworld.Navigate.GetPosition();
            _sgworld.Creator.CreateKMLLayer(startMarkerKmlLocation, groupID);
            _sgworld.Navigate.JumpTo(cameraPosition);

            DisEngageTool();
            return false;
        }

        public void DisEngageTool()
        {
            _sgworld.OnLButtonClicked -= Sgworld_OnLButtonClicked;
            _sgworld.Window.SetInputMode(MouseInputMode.MI_FREE_FLIGHT);
        }
    }
}