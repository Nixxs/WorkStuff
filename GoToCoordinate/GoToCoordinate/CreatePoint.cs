using System;
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

        public CreatePoint(SGWorld71 sgworld)
        {
            appdir = _sgworld.Application.DataPath;
            _sgworld = sgworld;
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