namespace CadRevealRvmProvider.Converters;

using CadRevealComposer.Primitives;
using CadRevealComposer.Utils;
using RvmSharp.Primitives;
using System.Diagnostics;
using System.Drawing;
using System.Numerics;

public static class ConverterExceptionHandling
{
    // TODO: Add logs where it is necessary and seen fit

    public static bool CanBeConverted(this RvmBox rvmBox, Vector3 scale, Quaternion rotation)
    {
        if (
            !(
                float.IsFinite(rotation.X)
                && float.IsFinite(rotation.Y)
                && float.IsFinite(rotation.Z)
                && float.IsFinite(rotation.W)
            )
        )
        {
            return false;
        }

        if ((rvmBox.LengthX <= 0 || rvmBox.LengthY <= 0 || rvmBox.LengthZ <= 0))
        {
            return false;
        }

        if (scale.X < 0 || scale.Y < 0 || scale.Z < 0)
            return false;

        Trace.Assert(scale.IsUniform(), $"Expected Uniform Scale For Box. Was: {scale}");

        if (!scale.X.ApproximatelyEquals(scale.Y, 0.0001))
        {
            Console.WriteLine("Warning: Found box with non-uniform X and Y scale");
        }
        return true;
    }

    public static bool CanBeConverted(
        this RvmCircularTorus rvmCircularTorus,
        Vector3 scale,
        Quaternion rotation,
        FailedPrimitivesLogObject? failedPrimitivesLogObject = null
    )
    {
        if (
            !(
                float.IsFinite(rotation.X)
                && float.IsFinite(rotation.Y)
                && float.IsFinite(rotation.Z)
                && float.IsFinite(rotation.W)
            )
        )
        {
            if (failedPrimitivesLogObject != null)
            {
                failedPrimitivesLogObject.FailedCircularToruses.RotationCounter++;
                return false;
            }
        }

        if (scale.X < 0 || scale.Y < 0 || scale.Z < 0)
        {
            if (failedPrimitivesLogObject != null)
                failedPrimitivesLogObject.FailedCircularToruses.ScaleCounter++;

            return false;
        }

        if (rvmCircularTorus.Radius <= 0)
        {
            if (failedPrimitivesLogObject != null)
                failedPrimitivesLogObject.FailedCircularToruses.RadiusCounter += 1;
            return false;
        }

        if (!float.IsFinite(rvmCircularTorus.Offset))
        {
            return false;
        }

        if (!float.IsFinite(rvmCircularTorus.Angle))
        {
            return false;
        }

        Trace.Assert(scale.IsUniform(), $"Expected Uniform Scale For Circular Torus. Was: {scale}");

        if (!scale.X.ApproximatelyEquals(scale.Y, 0.0001))
        {
            Console.WriteLine("Warning: Found circular torus with non-uniform X and Y scale");
        }

        return true;
    }

    public static bool CanBeConverted(
        this RvmCylinder rvmCylinder,
        Vector3 scale,
        Quaternion rotation,
        FailedPrimitivesLogObject? failedPrimitivesLogObject = null
    )
    {
        if (
            !(
                float.IsFinite(rotation.X)
                && float.IsFinite(rotation.Y)
                && float.IsFinite(rotation.Z)
                && float.IsFinite(rotation.W)
            )
        )
        {
            if (failedPrimitivesLogObject != null)
            {
                failedPrimitivesLogObject.FailedCylinders.RotationCounter++;
                return false;
            }
        }

        if (rvmCylinder.Radius <= 0)
        {
            if (failedPrimitivesLogObject != null)
            {
                failedPrimitivesLogObject.FailedCylinders.RadiusCounter++;
                return false;
            }
        }

        if (scale.X < 0 || scale.Y < 0 || scale.Z < 0)
        {
            if (failedPrimitivesLogObject != null)
                failedPrimitivesLogObject.FailedCylinders.ScaleCounter++;

            return false;
        }

        if (rvmCylinder.Height <= 0)
        {
            return false;
        }
        Trace.Assert(scale.IsUniform(), $"Expected Uniform Scale For Cylinder. Was: {scale}");

        if (!scale.X.ApproximatelyEquals(scale.Y, 0.0001))
        {
            Console.WriteLine("Warning: Found cylinder with non-uniform X and Y scale");
        }
        return true;
    }

    public static bool CanBeConverted(this RvmEllipticalDish rvmEllipticalDish, Vector3 scale, Quaternion rotation)
    {
        if (
            !(
                float.IsFinite(rotation.X)
                && float.IsFinite(rotation.Y)
                && float.IsFinite(rotation.Z)
                && float.IsFinite(rotation.W)
            )
        )
        {
            return false;
        }

        if (rvmEllipticalDish.BaseRadius <= 0)
        {
            return false;
        }

        if (rvmEllipticalDish.Height <= 0)
        {
            return false;
        }

        if (scale.X < 0 || scale.Y < 0 || scale.Z < 0)
            return false;

        Trace.Assert(scale.IsUniform(), $"Expected Uniform Scale For Elliptical Dish. Was: {scale}");

        if (!scale.X.ApproximatelyEquals(scale.Y, 0.0001))
        {
            Console.WriteLine("Warning: Found elliptical dish with non-uniform X and Y scale");
        }
        return true;
    }

    public static bool CanBeConverted(
        this RvmSnout rvmSnout,
        Vector3 scale,
        Quaternion rotation,
        FailedPrimitivesLogObject? failedPrimitivesLogObject = null
    )
    {
        if (rvmSnout.RadiusBottom <= 0 || rvmSnout.RadiusTop <= 0)
        {
            if (failedPrimitivesLogObject != null)
                failedPrimitivesLogObject.FailedSnouts.RadiusCounter++;

            return false;
        }

        if (scale.X < 0 || scale.Y < 0 || scale.Z < 0)
        {
            if (failedPrimitivesLogObject != null)
                failedPrimitivesLogObject.FailedSnouts.ScaleCounter++;

            return false;
        }

        if (
            !(
                float.IsFinite(rotation.X)
                && float.IsFinite(rotation.Y)
                && float.IsFinite(rotation.Z)
                && float.IsFinite(rotation.W)
            )
        )
        {
            if (failedPrimitivesLogObject != null)
            {
                failedPrimitivesLogObject.FailedSnouts.RotationCounter++;
                return false;
            }
        }

        if (
            !(
                float.IsFinite(rvmSnout.OffsetX)
                || float.IsFinite(rvmSnout.OffsetY)
                || float.IsFinite(rvmSnout.BottomShearX)
                || float.IsFinite(rvmSnout.BottomShearY)
                || float.IsFinite(rvmSnout.TopShearX)
                || float.IsFinite(rvmSnout.TopShearY)
            )
        )
        {
            return false;
        }

        if (rvmSnout.Height <= 0)
        {
            return false;
        }

        Trace.Assert(scale.IsUniform(), $"Expected Uniform Scale For Snout. Was: {scale}");

        if (!scale.X.ApproximatelyEquals(scale.Y, 0.0001))
        {
            Console.WriteLine("Warning: Found snout with non-uniform X and Y scale");
        }

        return true;
    }

    public static bool CanBeConverted(
        this RvmRectangularTorus rvmRectangularTorus,
        Vector3 scale,
        Quaternion rotation,
        FailedPrimitivesLogObject? failedPrimitivesLogObject = null
    )
    {
        if (rvmRectangularTorus.RadiusOuter <= 0 || rvmRectangularTorus.RadiusInner < 0)
        {
            if (failedPrimitivesLogObject != null)
                failedPrimitivesLogObject.FailedRectangularTorus.RadiusCounter++;

            return false;
        }

        if (scale.X < 0 || scale.Y < 0 || scale.Z < 0)
        {
            if (failedPrimitivesLogObject != null)
                failedPrimitivesLogObject.FailedRectangularTorus.ScaleCounter++;

            return false;
        }

        if (rvmRectangularTorus.Height <= 0 || !float.IsFinite(rvmRectangularTorus.Angle))
        {
            return false;
        }

        if (
            !(
                float.IsFinite(rotation.X)
                && float.IsFinite(rotation.Y)
                && float.IsFinite(rotation.Z)
                && float.IsFinite(rotation.W)
            )
        )
        {
            if (failedPrimitivesLogObject != null)
            {
                failedPrimitivesLogObject.FailedRectangularTorus.RotationCounter++;
                return false;
            }
        }

        Trace.Assert(scale.IsUniform(), $"Expected Uniform Scale. Was: {scale}");

        if (!scale.X.ApproximatelyEquals(scale.Y, 0.0001))
        {
            Console.WriteLine("Warning: Found snout with non-uniform X and Y scale");
        }

        return true;
    }

    public static bool CanBeConverted(this RvmPyramid rvmPyramid, Vector3 scale, Quaternion rotation)
    {
        if (scale.X < 0 || scale.Y < 0 || scale.Z < 0)
        {
            return false;
        }

        if (
            !(
                float.IsFinite(rotation.X)
                && float.IsFinite(rotation.Y)
                && float.IsFinite(rotation.Z)
                && float.IsFinite(rotation.W)
            )
        )
        {
            return false;
        }

        if (
            !(
                float.IsFinite(rvmPyramid.OffsetX)
                || float.IsFinite(rvmPyramid.OffsetY)
                || float.IsFinite(rvmPyramid.BottomX)
                || float.IsFinite(rvmPyramid.BottomY)
                || float.IsFinite(rvmPyramid.TopX)
                || float.IsFinite(rvmPyramid.TopY)
            )
        )
        {
            return false;
        }

        if (rvmPyramid.Height <= 0)
        {
            return false;
        }

        Trace.Assert(scale.IsUniform(), $"Expected Uniform Scale For Pyramid. Was: {scale}");

        if (!scale.X.ApproximatelyEquals(scale.Y, 0.0001))
        {
            Console.WriteLine("Warning: Found Pyramid with non-uniform X and Y scale");
        }

        return true;
    }

    public static bool CanBeConverted(this RvmSphere rvmSphere, Vector3 scale, Quaternion rotation)
    {
        if (scale.X < 0 || scale.Y < 0 || scale.Z < 0)
        {
            return false;
        }

        if (
            !(
                float.IsFinite(rotation.X)
                && float.IsFinite(rotation.Y)
                && float.IsFinite(rotation.Z)
                && float.IsFinite(rotation.W)
            )
        )
        {
            return false;
        }

        if (rvmSphere.Radius <= 0)
        {
            return false;
        }

        Trace.Assert(scale.IsUniform(), $"Expected Uniform Scale For Sphere. Was: {scale}");

        if (!scale.X.ApproximatelyEquals(scale.Y, 0.0001))
        {
            Console.WriteLine("Warning: Found Sphere with non-uniform X and Y scale");
        }

        return true;
    }

    public static bool CanBeConverted(this RvmSphericalDish rvmSphericalDish, Vector3 scale, Quaternion rotation)
    {
        if (scale.X < 0 || scale.Y < 0 || scale.Z < 0)
        {
            return false;
        }

        if (
            !(
                float.IsFinite(rotation.X)
                && float.IsFinite(rotation.Y)
                && float.IsFinite(rotation.Z)
                && float.IsFinite(rotation.W)
            )
        )
        {
            return false;
        }

        if (rvmSphericalDish.BaseRadius <= 0 || rvmSphericalDish.Height <= 0)
        {
            return false;
        }

        Trace.Assert(scale.IsUniform(), $"Expected Uniform Scale For Spherical Dish. Was: {scale}");

        if (!scale.X.ApproximatelyEquals(scale.Y, 0.0001))
        {
            Console.WriteLine("Warning: Found Spherical Dish with non-uniform X and Y scale");
        }

        return true;
    }
}
