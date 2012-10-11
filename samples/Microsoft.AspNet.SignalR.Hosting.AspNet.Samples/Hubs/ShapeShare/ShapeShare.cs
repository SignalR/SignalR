using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hubs;

namespace Microsoft.AspNet.SignalR.Samples.Hubs.ShapeShare
{
    public class ShapeShare : Hub
    {
        private static ConcurrentDictionary<string, User> _users = new ConcurrentDictionary<string, User>(StringComparer.OrdinalIgnoreCase);
        private static ConcurrentDictionary<string, Shape> _shapes = new ConcurrentDictionary<string, Shape>();
        private static Random _userNameGenerator = new Random();

        public IEnumerable<Shape> GetShapes()
        {
            return _shapes.Values;
        }

        public void Join(string userName)
        {
            User user = null;
            if (string.IsNullOrWhiteSpace(userName))
            {
                user = new User();
                do
                {
                    user.Name = "User" + _userNameGenerator.Next(1000);
                } while (!_users.TryAdd(user.Name, user));
            }
            else if (!_users.TryGetValue(userName, out user))
            {
                user = new User { Name = userName };
                _users.TryAdd(userName, user);
            }
            Clients.Caller.user = user;
        }

        public void ChangeUserName(string currentUserName, string newUserName)
        {
            User user;
            if (!string.IsNullOrEmpty(newUserName) && _users.TryGetValue(currentUserName, out user))
            {
                user.Name = newUserName;
                User oldUser;
                _users.TryRemove(currentUserName, out oldUser);
                _users.TryAdd(newUserName, user);
                Clients.All.userNameChanged(currentUserName, newUserName);
                Clients.Caller.user = user;
            }
        }

        public Task CreateShape(string type = "rectangle")
        {
            string name = Clients.Caller.user["Name"];
            var user = _users[name];
            var shape = Shape.Create(type);
            shape.ChangedBy = user;
            _shapes.TryAdd(shape.ID, shape);

            return Clients.All.shapeAdded(shape);
        }

        public void ChangeShape(string id, int x, int y, int w, int h)
        {
            if (w <= 0 || h <= 0) return;

            var shape = FindShape(id);
            if (shape == null)
            {
                return;
            }

            string name = Clients.Caller.user["Name"];
            var user = _users[name];

            shape.Width = w;
            shape.Height = h;
            shape.Location.X = x;
            shape.Location.Y = y;
            shape.ChangedBy = user;

            Task task = Clients.Others.shapeChanged(shape);
            task.Wait();
            if (task.Exception != null)
            {
                throw task.Exception;
            }
        }

        public void DeleteShape(string id)
        {
            var shape = FindShape(id);
            if (shape == null)
            {
                return;
            }

            Shape ignored;
            _shapes.TryRemove(id, out ignored);

            Clients.All.shapeDeleted(id);
        }

        public void DeleteAllShapes()
        {
            var shapes = _shapes.Select(s => new { id = s.Value.ID }).ToList();

            _shapes.Clear();

            Clients.All.shapesDeleted(shapes);
        }

        private Shape FindShape(string id)
        {
            return _shapes[id];
        }
    }

    public class User
    {
        public string ID { get; private set; }
        public string Name { get; set; }

        public User()
        {
            ID = Guid.NewGuid().ToString("d");
        }
    }

    public abstract class Shape
    {
        public string ID { get; private set; }
        public Point Location { get; set; }
        public virtual int Width { get; set; }
        public virtual int Height { get; set; }
        public string Type { get { return GetType().Name.ToLower(); } }
        public User ChangedBy { get; set; }

        public Shape()
        {
            ID = Guid.NewGuid().ToString("d");
            Location = new Point(20, 20);
        }

        public static Shape Create(string type)
        {
            switch (type)
            {
                case "picture":
                    return new Picture();
                case "circle":
                    return new Circle();
                case "square":
                    return new Square();
                case "rectangle":
                default:
                    return new Rectangle();
            }
        }
    }

    public abstract class WidthHeightFixed : Shape
    {
        public override int Height
        {
            get
            {
                return base.Height;
            }
            set
            {
                base.Height = value;
                base.Width = value;
            }
        }

        public override int Width
        {
            get
            {
                return base.Width;
            }
            set
            {
                base.Width = value;
                base.Height = value;
            }
        }
    }

    public class Rectangle : Shape
    {
        public Rectangle()
            : base()
        {
            Width = 160;
            Height = 100;
        }
    }

    public class Square : WidthHeightFixed
    {
        public Square()
            : base()
        {
            Width = 100;
        }
    }

    public class Circle : WidthHeightFixed
    {
        public int Radius
        {
            get { return base.Width / 2; }
            set
            {
                base.Height = value * 2;
                base.Width = value * 2;
            }
        }

        public Circle()
            : base()
        {
            Width = 100;
        }
    }

    public class Picture : Shape
    {
        public string Src { get; set; }

        public Picture()
            : base()
        {
            Src = "http://www.w3.org/html/logo/badge/html5-badge-h-css3-semantics.png";
            Width = 165;
            Height = 64;
        }
    }

    public class Point
    {
        public int X { get; set; }
        public int Y { get; set; }

        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }
    }
}