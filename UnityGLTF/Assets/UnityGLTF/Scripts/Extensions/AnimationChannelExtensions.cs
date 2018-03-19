using GLTF.Schema;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityGLTF.Cache;

namespace UnityGLTF.Extensions
{
	public static class AnimationChannelExtensions
	{
		public static AnimationCurve[] AsAnimationCurves(this AnimationChannel animationChannel, AnimationSamplerCacheData samplerCache)
		{
			float[] timeArray = samplerCache.Input.AccessorContent.AsFloats;
			AnimationCurve[] curves = GenerateCurvesArray(animationChannel, samplerCache);
			FillCurveData(animationChannel, samplerCache, curves, timeArray);
			//InputCurveInterpolation(curves);

			return curves;
		}

		// TODO: convert to extension method
		private static AnimationCurve[] GenerateCurvesArray(AnimationChannel animationChannel, AnimationSamplerCacheData samplerCache)
		{
			Node node = animationChannel.Target.Node.Value;
			int stride;
			if (IsBlendShapes(animationChannel))
				stride = node.Mesh.Value.Primitives[0].Targets.Count;
			else if (samplerCache.Output.AccessorId.Value.Type == GLTFAccessorAttributeType.VEC3)
				stride = 3;
			else if (samplerCache.Output.AccessorId.Value.Type == GLTFAccessorAttributeType.VEC4)
				stride = 4;
			else
				throw new GLTFTypeMismatchException("Animation sampler output points to invalidly-typed accessor " + samplerCache.Output.AccessorId.Value.Type);

			var curves = new AnimationCurve[stride];
			for (int i = 0; i < stride; i++)
				curves[i] = new AnimationCurve();

			return curves;
		}

		// TODO: convert to extension method
		// TODO: remove - can be part of the switch statement
		private static bool IsBlendShapes(AnimationChannel animationChannel)
		{
			return animationChannel.Target.Path == GLTFAnimationChannelPath.weights;
		}

		private static void FillCurveData(AnimationChannel animationChannel, AnimationSamplerCacheData samplerCache, AnimationCurve[] curves, float[] timeArray)
		{
			// TODO: conver to a switch statement
			if (IsBlendShapes(animationChannel))
				FillBlendShapeCurveData(animationChannel, samplerCache, curves, timeArray);
			else if (animationChannel.Target.Path == GLTFAnimationChannelPath.translation)
				FillTranslationCurveData(curves, samplerCache, timeArray);
			else if (animationChannel.Target.Path == GLTFAnimationChannelPath.rotation)
				FillRotationCurveData(curves, samplerCache, timeArray);
			else if (animationChannel.Target.Path == GLTFAnimationChannelPath.scale)
				FillScaleData(curves, samplerCache, timeArray);
		}

		private static void FillBlendShapeCurveData(AnimationChannel animationChannel, AnimationSamplerCacheData samplerCache, AnimationCurve[] curves, float[] timeArray)
		{
			Node node = animationChannel.Target.Node.Value;
			int numTargets = node.Mesh.Value.Primitives[0].Targets.Count;
			float[] animArray = samplerCache.Output.AccessorContent.AsFloats;

			for (int timeIdx = 0; timeIdx < timeArray.Length; timeIdx++)
				for (int targetIndex = 0; targetIndex < numTargets; targetIndex++)
					curves[targetIndex].AddKey(new Keyframe(timeArray[timeIdx], animArray[numTargets * timeIdx + targetIndex]));
		}

		private static void FillTranslationCurveData(AnimationCurve[] curves, AnimationSamplerCacheData samplerCache, float[] timeArray)
		{
			Vector3[] animArray = samplerCache.Output.AccessorContent.AsVec3s.ToUnityVector3Convert();
			for (int timeIdx = 0; timeIdx < timeArray.Length; timeIdx++)
			{
				curves[0].AddKey(new Keyframe(timeArray[timeIdx], animArray[timeIdx].x));
				curves[1].AddKey(new Keyframe(timeArray[timeIdx], animArray[timeIdx].y));
				curves[2].AddKey(new Keyframe(timeArray[timeIdx], animArray[timeIdx].z));
			}
		}

		private static void FillRotationCurveData(AnimationCurve[] curves, AnimationSamplerCacheData samplerCache, float[] timeArray)
		{
			Quaternion[] animArray = samplerCache.Output.AccessorContent.AsVec4s.ToUnityQuaternionConvert();
			for (int timeIdx = 0; timeIdx < timeArray.Length; timeIdx++)
			{
				curves[0].AddKey(new Keyframe(timeArray[timeIdx], animArray[timeIdx].x));
				curves[1].AddKey(new Keyframe(timeArray[timeIdx], animArray[timeIdx].y));
				curves[2].AddKey(new Keyframe(timeArray[timeIdx], animArray[timeIdx].z));
				curves[3].AddKey(new Keyframe(timeArray[timeIdx], animArray[timeIdx].w));
			}
		}

		private static void FillScaleData(AnimationCurve[] curves, AnimationSamplerCacheData samplerCache, float[] timeArray)
		{
			Vector3[] animArray = samplerCache.Output.AccessorContent.AsVec3s.ToUnityVector3Raw();
			for (int timeIdx = 0; timeIdx < timeArray.Length; timeIdx++)
			{
				curves[0].AddKey(new Keyframe(timeArray[timeIdx], animArray[timeIdx].x));
				curves[1].AddKey(new Keyframe(timeArray[timeIdx], animArray[timeIdx].y));
				curves[2].AddKey(new Keyframe(timeArray[timeIdx], animArray[timeIdx].z));
			}
		}
		/* TODO： add interpolation functions
		private static void InputCurveInterpolation(AnimationCurve[] curves, AnimationSamplerCacheData samplerCache)
		{
			// TODO: should be a switch statement
			if (Sampler.Value.Interpolation == InterpolationType.LINEAR)
				for (int i = 0; i < curves.Length; i++)
					LinearizeCurve(curves[i]);

			if (Sampler.Value.Interpolation == InterpolationType.STEP)
				for (int i = 0; i < curves.Length; i++)
					StepCurve(curves[i]);
		}
		*/
		private static void LinearizeCurve(AnimationCurve curve)
		{
			for (int timeIdx = 0; timeIdx < curve.length; timeIdx++)
			{
				Keyframe key = curve[timeIdx];
				if (timeIdx >= 1)
					key.inTangent = CalculateLinearTangent(curve[timeIdx - 1].value, curve[timeIdx].value, curve[timeIdx - 1].time, curve[timeIdx].time);

				if (timeIdx + 1 < curve.length)
					key.outTangent = CalculateLinearTangent(curve[timeIdx].value, curve[timeIdx + 1].value, curve[timeIdx].time, curve[timeIdx + 1].time);

				curve.MoveKey(timeIdx, key);
			}
		}

		private static void StepCurve(AnimationCurve curve)
		{
			for (int timeIdx = 0; timeIdx < curve.length; timeIdx++)
			{
				Keyframe key = curve[timeIdx];
				if (timeIdx >= 1)
					key.inTangent = float.PositiveInfinity;

				if (timeIdx + 1 < curve.length)
					key.outTangent = float.PositiveInfinity;

				curve.MoveKey(timeIdx, key);
			}
		}

		private static float CalculateLinearTangent(float valueStart, float valueEnd, float timeStart, float timeEnd)
		{
			return (valueEnd - valueStart) / (timeEnd - timeStart);
		}
	}
}
