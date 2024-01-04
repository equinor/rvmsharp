namespace CadRevealRvmProvider.Converters;

using CadRevealComposer.Utils;
using RvmSharp.Primitives;
using System.Diagnostics;
using System.Numerics;

public static class ConverterExceptionHandling
{
    public static bool CanBeConverted(
        this RvmBox rvmBox,
        Vector3 scale,
        Quaternion rotation,
        FailedPrimitivesLogObject failedPrimitivesLogObject
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
            failedPrimitivesLogObject.FailedBoxes.RotationCounter++;
            return false;
        }

        if ((rvmBox.LengthX <= 0 || rvmBox.LengthY <= 0 || rvmBox.LengthZ <= 0))
        {
            if (failedPrimitivesLogObject != null)
                failedPrimitivesLogObject.FailedBoxes.SizeCounter++;

            return false;
        }

        if (scale.X < 0 || scale.Y < 0 || scale.Z < 0)
        {
            failedPrimitivesLogObject.FailedBoxes.ScaleCounter++;
            return false;
        }

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
        FailedPrimitivesLogObject failedPrimitivesLogObject
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
            failedPrimitivesLogObject.FailedCircularToruses.RotationCounter++;
            return false;
        }

        if (scale.X < 0 || scale.Y < 0 || scale.Z < 0)
        {
            failedPrimitivesLogObject.FailedCircularToruses.ScaleCounter++;
            return false;
        }

        if (rvmCircularTorus.Radius <= 0)
        {
            failedPrimitivesLogObject.FailedCircularToruses.SizeCounter++;
            return false;
        }

        if (!float.IsFinite(rvmCircularTorus.Offset))
        {
            failedPrimitivesLogObject.FailedCircularToruses.SizeCounter++;

            return false;
        }

        if (!float.IsFinite(rvmCircularTorus.Angle))
        {
            failedPrimitivesLogObject.FailedCircularToruses.RotationCounter++;
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
        FailedPrimitivesLogObject failedPrimitivesLogObject
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
            failedPrimitivesLogObject.FailedCylinders.RotationCounter++;
            return false;
        }

        if (rvmCylinder.Radius <= 0)
        {
            failedPrimitivesLogObject.FailedCylinders.SizeCounter++;
            return false;
        }

        if (scale.X < 0 || scale.Y < 0 || scale.Z < 0)
        {
            failedPrimitivesLogObject.FailedCylinders.ScaleCounter++;
        }

        if (rvmCylinder.Height <= 0)
        {
            failedPrimitivesLogObject.FailedCylinders.SizeCounter++;
            return false;
        }
        Trace.Assert(scale.IsUniform(), $"Expected Uniform Scale For Cylinder. Was: {scale}");

        if (scale.X != 0 && scale.Y == 0)
        {
            Console.WriteLine("Warning: Found cylinder where X scale was non-zero and Y scale was zero");
        }
        else if (!scale.X.ApproximatelyEquals(scale.Y, 0.0001))
        {
            throw new Exception("Cylinders with non-uniform scale is not implemented!");
        }
        return true;
    }

    public static bool CanBeConverted(
        this RvmEllipticalDish rvmEllipticalDish,
        Vector3 scale,
        Quaternion rotation,
        FailedPrimitivesLogObject failedPrimitivesLogObject
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
            return false;
        }

        if (rvmEllipticalDish.BaseRadius <= 0)
        {
            failedPrimitivesLogObject.FailedEllipticalDishes.SizeCounter++;
            return false;
        }

        if (rvmEllipticalDish.Height <= 0)
            failedPrimitivesLogObject.FailedEllipticalDishes.SizeCounter++;
        return false;

        if (scale.X < 0 || scale.Y < 0 || scale.Z < 0)
            failedPrimitivesLogObject.FailedEllipticalDishes.ScaleCounter++;
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
        FailedPrimitivesLogObject failedPrimitivesLogObject
    )
    {
        if (rvmSnout.RadiusBottom <= 0 || rvmSnout.RadiusTop <= 0)
        {
            failedPrimitivesLogObject.FailedSnouts.SizeCounter++;
            return false;
        }

        if (scale.X < 0 || scale.Y < 0 || scale.Z < 0)
        {
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
            failedPrimitivesLogObject.FailedSnouts.RotationCounter++;
            return false;
        }

        if (
            !(
                float.IsFinite(rvmSnout.OffsetX)
                && float.IsFinite(rvmSnout.OffsetY)
                && float.IsFinite(rvmSnout.BottomShearX)
                && float.IsFinite(rvmSnout.BottomShearY)
                && float.IsFinite(rvmSnout.TopShearX)
                && float.IsFinite(rvmSnout.TopShearY)
            )
        )
        {
            failedPrimitivesLogObject.FailedSnouts.SizeCounter++;
            return false;
        }

        if (rvmSnout.Height <= 0)
        {
            failedPrimitivesLogObject.FailedSnouts.SizeCounter++;

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
        FailedPrimitivesLogObject failedPrimitivesLogObject
    )
    {
        if (
            rvmRectangularTorus.RadiusOuter <= 0
            || rvmRectangularTorus.RadiusInner < 0
            || rvmRectangularTorus.Height <= 0
        )
        {
            failedPrimitivesLogObject.FailedRectangularTorus.SizeCounter++;
            return false;
        }

        if (scale.X < 0 || scale.Y < 0 || scale.Z < 0)
        {
            failedPrimitivesLogObject.FailedRectangularTorus.ScaleCounter++;
            return false;
        }

        if (!float.IsFinite(rvmRectangularTorus.Angle))
            failedPrimitivesLogObject.FailedRectangularTorus.RotationCounter++;
        return false;

        if (
            !(
                float.IsFinite(rotation.X)
                && float.IsFinite(rotation.Y)
                && float.IsFinite(rotation.Z)
                && float.IsFinite(rotation.W)
            )
        )
        {
            failedPrimitivesLogObject.FailedRectangularTorus.RotationCounter++;
            return false;
        }

        Trace.Assert(scale.IsUniform(), $"Expected Uniform Scale. Was: {scale}");

        if (!scale.X.ApproximatelyEquals(scale.Y, 0.0001))
        {
            Console.WriteLine("Warning: Found snout with non-uniform X and Y scale");
        }

        return true;
    }

    public static bool CanBeConverted(
        this RvmPyramid rvmPyramid,
        Vector3 scale,
        Quaternion rotation,
        FailedPrimitivesLogObject failedPrimitivesLogObject
    )
    {
        if (scale.X < 0 || scale.Y < 0 || scale.Z < 0)
        {
            failedPrimitivesLogObject.FailedPyramids.ScaleCounter++;
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
            failedPrimitivesLogObject.FailedPyramids.RotationCounter++;
            return false;
        }

        if (
            !(
                float.IsFinite(rvmPyramid.OffsetX)
                && float.IsFinite(rvmPyramid.OffsetY)
                && float.IsFinite(rvmPyramid.BottomX)
                && float.IsFinite(rvmPyramid.BottomY)
                && float.IsFinite(rvmPyramid.TopX)
                && float.IsFinite(rvmPyramid.TopY)
            )
        )
        {
            failedPrimitivesLogObject.FailedPyramids.SizeCounter++;
            return false;
        }

        if (rvmPyramid.Height <= 0)
        {
            failedPrimitivesLogObject.FailedPyramids.SizeCounter++;
            return false;
        }

        Trace.Assert(scale.IsUniform(), $"Expected Uniform Scale For Pyramid. Was: {scale}");

        if (!scale.X.ApproximatelyEquals(scale.Y, 0.0001))
        {
            Console.WriteLine("Warning: Found Pyramid with non-uniform X and Y scale");
        }

        return true;
    }

    public static bool CanBeConverted(
        this RvmSphere rvmSphere,
        Vector3 scale,
        Quaternion rotation,
        FailedPrimitivesLogObject failedPrimitivesLogObject
    )
    {
        if (scale.X < 0 || scale.Y < 0 || scale.Z < 0)
        {
            failedPrimitivesLogObject.FailedSpheres.ScaleCounter++;
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
            failedPrimitivesLogObject.FailedSpheres.RotationCounter++;
            return false;
        }

        if (rvmSphere.Radius <= 0)
        {
            failedPrimitivesLogObject.FailedSpheres.SizeCounter++;
            return false;
        }

        Trace.Assert(scale.IsUniform(), $"Expected Uniform Scale For Sphere. Was: {scale}");

        if (!scale.X.ApproximatelyEquals(scale.Y, 0.0001))
        {
            Console.WriteLine("Warning: Found Sphere with non-uniform X and Y scale");
        }

        return true;
    }

    public static bool CanBeConverted(
        this RvmSphericalDish rvmSphericalDish,
        Vector3 scale,
        Quaternion rotation,
        FailedPrimitivesLogObject failedPrimitivesLogObject
    )
    {
        if (scale.X < 0 || scale.Y < 0 || scale.Z < 0)
        {
            failedPrimitivesLogObject.FailedSphericalDishes.ScaleCounter++;
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
            failedPrimitivesLogObject.FailedSphericalDishes.RotationCounter++;
            return false;
        }

        if (rvmSphericalDish.BaseRadius <= 0)
        {
            failedPrimitivesLogObject.FailedSphericalDishes.SizeCounter++;
            return false;
        }

        if (rvmSphericalDish.Height <= 0)
        {
            failedPrimitivesLogObject.FailedSphericalDishes.SizeCounter++;
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
