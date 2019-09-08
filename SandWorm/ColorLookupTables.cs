using System.Drawing;


namespace SandWorm
{
    class ColorLookupTables
    {
        private readonly Color[] _lookupTables = new Color[6];

        public void PopulateTables()
        {

            _lookupTables[0] = Color.FromArgb(128, 128, 128);
            _lookupTables[1] = Color.FromArgb(255, 128, 128);
            _lookupTables[2] = Color.FromArgb(255, 255, 128);
            _lookupTables[3] = Color.FromArgb(255, 128, 255);
            _lookupTables[4] = Color.FromArgb(255, 0, 128);
            _lookupTables[5] = Color.FromArgb(255, 0, 117);

        }

        public Color PickGradient(int index)
        {
            return _lookupTables[index];
        }
    }
}
