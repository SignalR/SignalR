using System.Threading;
using SignalR.Hubs;

namespace SignalR.Hosting.AspNet.Samples.Hubs.DrawingPad
{
    [HubName("DrawingPad")]
    public class DrawingPad : Hub
    {

        #region Data structures

        public class Point
        {
            public int X { get; set; }
            public int Y { get; set; }
        }
        public class Line
        {
            public Point From { get; set; }
            public Point To { get; set; }
            public string Color { get; set; }
        }
        #endregion

        private static long _id;
        // defines some colors
        private readonly static string[] colors = new string[]{
            "red", "green", "blue", "orange", "navy", "silver", "black", "lime"
        };

        public void Join()
        {
            Caller.id = Interlocked.Increment(ref _id);
            Caller.color = colors[_id % colors.Length];
        }

        // A user has drawed a line ...
        public void DrawLine(Line data)
        {            
            // ... propagate it to all users
            Clients.lineDrawed(Caller.id, data);
        }
    }
}