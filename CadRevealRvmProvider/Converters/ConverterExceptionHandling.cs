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
        if (IsRotationValid(rotation))
        {
            failedPrimitivesLogObject.FailedBoxes.RotationCounter++;
            return false;
        }

        if ((rvmBox.LengthX <= 0 || rvmBox.LengthY <= 0 || rvmBox.LengthZ <= 0))
        {
            failedPrimitivesLogObject.FailedBoxes.SizeCounter++;
            return false;
        }

        if (IsScaleValid(scale))
        {
            failedPrimitivesLogObject.FailedBoxes.ScaleCounter++;
            return false;
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
        if (IsRotationValid(rotation))
        {
            failedPrimitivesLogObject.FailedCircularToruses.RotationCounter++;
            return false;
        }

        if (IsScaleValid(scale))
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

        return true;
    }

    public static bool CanBeConverted(
        this RvmCylinder rvmCylinder,
        Vector3 scale,
        Quaternion rotation,
        FailedPrimitivesLogObject failedPrimitivesLogObject
    )
    {
        if (IsRotationValid(rotation))
        {
            failedPrimitivesLogObject.FailedCylinders.RotationCounter++;
            return false;
        }

        if (rvmCylinder.Radius <= 0)
        {
            failedPrimitivesLogObject.FailedCylinders.SizeCounter++;
            return false;
        }

        if (IsScaleValid(scale))
        {
            failedPrimitivesLogObject.FailedCylinders.ScaleCounter++;
        }

        if (rvmCylinder.Height <= 0)
        {
            failedPrimitivesLogObject.FailedCylinders.SizeCounter++;
            return false;
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
        if (IsRotationValid(rotation))
        {
            return false;
        }

        if (rvmEllipticalDish.BaseRadius <= 0)
        {
            failedPrimitivesLogObject.FailedEllipticalDishes.SizeCounter++;
            return false;
        }

        if (rvmEllipticalDish.Height <= 0)
        {
            failedPrimitivesLogObject.FailedEllipticalDishes.SizeCounter++;
            return false;
        }

        if (IsScaleValid(scale))
        {
            failedPrimitivesLogObject.FailedEllipticalDishes.ScaleCounter++;
            return false;
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

        if (IsScaleValid(scale))
        {
            failedPrimitivesLogObject.FailedSnouts.ScaleCounter++;
            return false;
        }

        if (IsRotationValid(rotation))
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

        if (IsScaleValid(scale))
        {
            failedPrimitivesLogObject.FailedRectangularTorus.ScaleCounter++;
            return false;
        }

        if (!float.IsFinite(rvmRectangularTorus.Angle))
        {
            failedPrimitivesLogObject.FailedRectangularTorus.RotationCounter++;
            return false;
        }

        if (IsRotationValid(rotation))
        {
            failedPrimitivesLogObject.FailedRectangularTorus.RotationCounter++;
            return false;
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
        if (IsScaleValid(scale))
        {
            failedPrimitivesLogObject.FailedPyramids.ScaleCounter++;
            return false;
        }

        if (IsRotationValid(rotation))
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

        return true;
    }

    public static bool CanBeConverted(
        this RvmSphere rvmSphere,
        Vector3 scale,
        Quaternion rotation,
        FailedPrimitivesLogObject failedPrimitivesLogObject
    )
    {
        if (IsScaleValid(scale))
        {
            failedPrimitivesLogObject.FailedSpheres.ScaleCounter++;
            return false;
        }

        if (IsRotationValid(rotation))
        {
            failedPrimitivesLogObject.FailedSpheres.RotationCounter++;
            return false;
        }

        if (rvmSphere.Radius <= 0)
        {
            failedPrimitivesLogObject.FailedSpheres.SizeCounter++;
            return false;
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
        if (IsScaleValid(scale))
        {
            failedPrimitivesLogObject.FailedSphericalDishes.ScaleCounter++;
            return false;
        }

        if (IsRotationValid(rotation))
        {
            failedPrimitivesLogObject.FailedSphericalDishes.RotationCounter++;
            return false;
        }

        if (rvmSphericalDish.BaseRadius <= 0 || rvmSphericalDish.Height <= 0)
        {
            failedPrimitivesLogObject.FailedSphericalDishes.SizeCounter++;
            return false;
        }

        return true;
    }

    private static bool IsScaleValid(Vector3 scale)
    {
        return scale.X < 0 || scale.Y < 0 || scale.Z < 0 || !scale.IsUniform();
    }

    private static bool IsRotationValid(Quaternion rotation)
    {
        return !(
            float.IsFinite(rotation.X)
            && float.IsFinite(rotation.Y)
            && float.IsFinite(rotation.Z)
            && float.IsFinite(rotation.W)
        );
    }
}
