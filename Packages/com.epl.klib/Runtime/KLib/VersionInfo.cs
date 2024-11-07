namespace KLib
{
    public class VersionInfo
    {
        private string _appName = "AppName";
        private int _major = 1;
        private int _minor = 0;
        private int _fix = 0;

        public VersionInfo() { }
        public VersionInfo(string name, int major, int minor, int fix)
        {
            _appName = name;
            _major = major;
            _minor = minor;
            _fix = fix;
        }

        public string AppName { get; private set; }

        public string SemanticVersion
        {
            get { return this.ToString(); }
        }

        public override string ToString()
        {
            string v = _major + "." + _minor;
            if (_fix > 0)
            {
                v += "." + _fix;
            }
            return v;
        }

        public static VersionInfo FromString(string verString)
        {
            var vi = new VersionInfo();

            verString = verString.Replace('-', '.');
            var parts = verString.Split('.');

            if (parts.Length == 1)
            {
                vi._major = 0;
                vi._minor = 0;
                vi._fix = int.Parse(parts[0]);
            }
            else
            {
                vi._major = int.Parse(parts[0]);
                vi._minor = int.Parse(parts[1]);
                if (parts.Length > 2)
                {
                    vi._fix = int.Parse(parts[2]);
                }
            }

            return vi;
        }

        public static int Compare(string test)
        {
            return Compare(VersionInfo.FromString(test), new VersionInfo());
        }

        public static int Compare(string test, string reference)
        {
            return Compare(VersionInfo.FromString(test), VersionInfo.FromString(reference));
        }

        public static int Compare(VersionInfo test, VersionInfo reference)
        {
            int result = 0;

            if (test._major < reference._major)
            {
                result = -1;
            }
            else if (test._major > reference._major)
            {
                result = 1;
            }
            else
            {
                if (test._minor < reference._minor)
                {
                    result = -1;
                }
                else if (test._minor > reference._minor)
                {
                    result = 1;
                }
                else
                {
                    if (test._fix < reference._fix)
                    {
                        result = -1;
                    }
                    else if (test._fix > reference._fix)
                    {
                        result = 1;
                    }
                }
            }

            return result;
        }
    }
}