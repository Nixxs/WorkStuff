using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerraExplorerX;
using System.Threading;

namespace GoToCoordinate
{
    class Heading
    {
        private SGWorld66 _sgworld;
        private int numClicks;
        private string lineWKT;
        private double lat1;
        private double lon1;
        private double lat2;
        private double lon2;
        private string groupID;
        private string appdir;
        private string markerImage;

        public Heading(SGWorld66 sgworld)
        {
            _sgworld = sgworld;
            numClicks = 0;
            lineWKT = "";
            appdir = _sgworld.Application.DataPath;
            markerImage = appdir + "\\Add-ons\\GoToCoordinate\\marker.png";
            groupID = "";
        }

        public void EngageTool()
        {
            _sgworld.OnLButtonClicked += Sgworld_OnLButtonClicked;
            _sgworld.Window.SetInputMode(MouseInputMode.MI_COM_CLIENT);
            _sgworld.Window.ShowMessageBarText("Click two points on the terrain to calculate bearing and distance.", MessageBarTextAlignment.MBT_CENTER, 7000);

            groupID = _sgworld.ProjectTree.CreateGroup("Bearing", "");
        }

        public void DisEngageTool()
        {
            numClicks = 0;
            lineWKT = "";
            _sgworld.OnLButtonClicked -= Sgworld_OnLButtonClicked;

            _sgworld.Window.SetInputMode(MouseInputMode.MI_FREE_FLIGHT);
        }

        private bool Sgworld_OnLButtonClicked(int Flags, int X, int Y)
        {
            numClicks += 1;
            Console.WriteLine(string.Format("Flags:{0} X:{1} Y:{2}", Flags, X, Y));
            IWorldPointInfo66 wgs84_position = _sgworld.Window.PixelToWorld(X, Y, WorldPointType.WPT_TERRAIN);

            // first click
            if (numClicks == 1)
            {
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
                IPosition66 cameraPosition = _sgworld.Navigate.GetPosition();
                _sgworld.Creator.CreateKMLLayer(startMarkerKmlLocation, groupID);
                _sgworld.Navigate.JumpTo(cameraPosition);
                
                lineWKT = string.Format("{0},{1},0 ", lon1, lat1);
            }

            // second click
            if (numClicks > 1)
            {
                lat2 = wgs84_position.Position.Y;
                lon2 = wgs84_position.Position.X;

                string endMarkerName = "END";
                string endMarkerKmlLocation = appdir + "\\Add-ons\\GoToCoordinate\\BearingEnd.kml";

                string endKMLText = $@"<?xml version='1.0' encoding='UTF-8'?>
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
		                <name>{endMarkerName}</name>
		                <styleUrl>#CoordinateMarker</styleUrl>
		                <Point>
			                <coordinates>{lon2},{lat2},0</coordinates>
		                </Point>
	                </Placemark>
                </Document>
                </kml>";
                File.WriteAllText(endMarkerKmlLocation, endKMLText);
                _sgworld.Creator.CreateKMLLayer(endMarkerKmlLocation, groupID);

                string lineKmlLocation = appdir + "\\Add-ons\\GoToCoordinate\\BearingLine.kml";
                lineWKT = string.Format("{0}{1},{2},0", lineWKT, lon2, lat2);
                string lineKMLText = $@"<?xml version='1.0' encoding='UTF-8'?>
                <kml xmlns='http://www.opengis.net/kml/2.2' xmlns:gx='http://www.google.com/kml/ext/2.2' xmlns:kml='http://www.opengis.net/kml/2.2' xmlns:atom='http://www.w3.org/2005/Atom'>
                <Document>
	                <name>Line.kml</name>
	                <Style id='LineStyle'>
		                <LineStyle>
			                <color>ffa26c00</color>
			                <width>5</width>
		                </LineStyle>
	                </Style>
	                <Placemark>
		                <name>Bearing Line</name>
		                <styleUrl>#LineStyle</styleUrl>
		                <LineString>
			                <tessellate>1</tessellate>
			                <coordinates>
				                {lineWKT}
			                </coordinates>
		                </LineString>
	                </Placemark>
                </Document>
                </kml>";
                File.WriteAllText(lineKmlLocation, lineKMLText);
                _sgworld.Creator.CreateKMLLayer(lineKmlLocation, groupID);

                // calculate heading
                double heading = GISTools.DegreeBearing(lat1, lon1, lat2, lon2);

                // calculate mid point
                Coordinate midpoint = GISTools.midPoint(lat1, lon1, lat2, lon2);

                string midPointMarkername = string.Format("Bearing: {0:f2}°", heading);
                string midPointKmlLocation = appdir + "\\Add-ons\\GoToCoordinate\\MidPoint.kml";
                string noMarkerIcon = appdir + "\\Add-ons\\GoToCoordinate\\nomarker.png";
                string midPointKMLText = $@"<?xml version='1.0' encoding='UTF-8'?>
                <kml xmlns='http://www.opengis.net/kml/2.2' xmlns:gx='http://www.google.com/kml/ext/2.2' xmlns:kml='http://www.opengis.net/kml/2.2' xmlns:atom='http://www.w3.org/2005/Atom'>
                <Document>
	                <Style id='CoordinateMarker'>
		                <IconStyle>
			                <scale>1.0</scale>
			                <Icon>
				                <href>{noMarkerIcon}</href>
			                </Icon>
			                <hotSpot x='0.5' y='1' xunits='fraction' yunits='fraction'/>
		                </IconStyle>
	                </Style>
	                <Placemark>
		                <name>{midPointMarkername}</name>
		                <styleUrl>#CoordinateMarker</styleUrl>
		                <Point>
			                <coordinates>{midpoint.X},{midpoint.Y},0</coordinates>
		                </Point>
	                </Placemark>
                </Document>
                </kml>";
                File.WriteAllText(midPointKmlLocation, midPointKMLText);
                _sgworld.Creator.CreateKMLLayer(midPointKmlLocation, groupID);

                // disengage the tool
                _sgworld.ProjectTree.RenameGroup(groupID, string.Format("Bearing: {0:f2}°", heading));
                _sgworld.ProjectTree.LockGroup(groupID, true);

                DisEngageTool();
            }

            return false;
        }
    }
}
