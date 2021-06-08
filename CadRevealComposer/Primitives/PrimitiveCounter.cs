namespace CadRevealComposer.Primitives
{
    public static class PrimitiveCounter
    {
        public static int pc = 0;
        public static int boxCounter = 0;
        public static int cTorus = 0;
        public static int cylinder = 0;
        public static int eDish = 0;
        public static int mesh = 0;
        public static int line = 0;
        public static int pyramid = 0;
        public static int rTorus = 0;
        public static int snout = 0;
        public static int sphere = 0;
        public static int sDish = 0;

        public static string ToString()
        {
            return $"{nameof(pc)}: {pc}, {nameof(boxCounter)}: {boxCounter}, {nameof(cTorus)}: {cTorus}, {nameof(cylinder)}: {cylinder}, {nameof(eDish)}: {eDish}, {nameof(mesh)}: {mesh}, {nameof(line)}: {line}, {nameof(pyramid)}: {pyramid}, {nameof(rTorus)}: {rTorus}, {nameof(snout)}: {snout}, {nameof(sphere)}: {sphere}, {nameof(sDish)}: {sDish}";
        }
    }
}