using UnityEngine;

namespace Utilities
{
    public static class SpringMotion
    {
        // From: https://gist.github.com/chadcable/92bc3958af5b171e593e36be57ca36ce
        
        // Reference video: 
        // https://www.youtube.com/watch?v=bFOAipGJGA0
        // Instant "Game Feel" Tutorial - Secrets of Springs Explained (by Toyful Games)
        // The channel LlamAcademy also made an adaption of the concept for Unity, 
        // "Add JUICE to Your Game with Springs | Unity Tutorial" - you can watch it here: 
        // https://www.youtube.com/watch?v=6mR7NSsi91Y

        // Copyright notice of the original source:
        /******************************************************************************
        Copyright (c) 2008-2012 Ryan Juckett
        http://www.ryanjuckett.com/

        This software is provided 'as-is', without any express or implied
        warranty. In no event will the authors be held liable for any damages
        arising from the use of this software.

        Permission is granted to anyone to use this software for any purpose,
        including commercial applications, and to alter it and redistribute it
        freely, subject to the following restrictions:

        1. The origin of this software must not be misrepresented; you must not
            claim that you wrote the original software. If you use this software
            in a product, an acknowledgment in the product documentation would be
            appreciated but is not required.

        2. Altered source versions must be plainly marked as such, and must not be
            misrepresented as being the original software.

        3. This notice may not be removed or altered from any source
            distribution.
        ******************************************************************************/

        // This file uses altered parts from the source: Comments are kept from the 
        // original article (for the most part) while the code has been translated from
        // c++ to C# using UnityEngine Mathf methods. The original source can be found
        // in this article: https://www.ryanjuckett.com/damped-springs/
        // Permissions and conditions from the original license are retained. 


        //******************************************************************************
        // Cached set of motion parameters that can be used to efficiently update
        // multiple springs using the same time step, angular frequency and damping
        // ratio.
        //******************************************************************************
        public struct DampedSpringMotionParams
        {
            // newPos = posPosCoef*oldPos + posVelCoef*oldVel
            public float posPosCoef, posVelCoef;
            // newVel = velPosCoef*oldPos + velVelCoef*oldVel
            public float velPosCoef, velVelCoef;
        };

        //******************************************************************************
        // This function will compute the parameters needed to simulate a damped spring
        // over a given period of time.
        // - An angular frequency is given to control how fast the spring oscillates.
        // - A damping ratio is given to control how fast the motion decays.
        //     damping ratio > 1: over damped
        //     damping ratio = 1: critically damped
        //     damping ratio < 1: under damped
        //******************************************************************************
        private static DampedSpringMotionParams CalcDampedSpringMotionParams(
            float deltaTime,        // time step to advance
            float angularFrequency, // angular frequency of motion
            float dampingRatio)     // damping ratio of motion
        {
            const float epsilon = 0.0001f;
            DampedSpringMotionParams pOutParams;

            // force values into legal range
            if (dampingRatio < 0.0f) dampingRatio = 0.0f;
            if (angularFrequency < 0.0f) angularFrequency = 0.0f;

            // if there is no angular frequency, the spring will not move and we can
            // return identity
            if (angularFrequency < epsilon)
            {
                pOutParams.posPosCoef = 1.0f; pOutParams.posVelCoef = 0.0f;
                pOutParams.velPosCoef = 0.0f; pOutParams.velVelCoef = 1.0f;
                return pOutParams;
            }

            if (dampingRatio > 1.0f + epsilon)
            {
                // over-damped
                float za = -angularFrequency * dampingRatio;
                float zb = angularFrequency * Mathf.Sqrt(dampingRatio * dampingRatio - 1.0f);
                float z1 = za - zb;
                float z2 = za + zb;
                // Value e (2.7) raised to a specific power
                float e1 = Mathf.Exp(z1 * deltaTime);
                float e2 = Mathf.Exp(z2 * deltaTime);

                float invTwoZb = 1.0f / (2.0f * zb); // = 1 / (z2 - z1)

                float e1_Over_TwoZb = e1 * invTwoZb;
                float e2_Over_TwoZb = e2 * invTwoZb;

                float z1e1_Over_TwoZb = z1 * e1_Over_TwoZb;
                float z2e2_Over_TwoZb = z2 * e2_Over_TwoZb;

                pOutParams.posPosCoef = e1_Over_TwoZb * z2 - z2e2_Over_TwoZb + e2;
                pOutParams.posVelCoef = -e1_Over_TwoZb + e2_Over_TwoZb;

                pOutParams.velPosCoef = (z1e1_Over_TwoZb - z2e2_Over_TwoZb + e2) * z2;
                pOutParams.velVelCoef = -z1e1_Over_TwoZb + z2e2_Over_TwoZb;
            }
            else if (dampingRatio < 1.0f - epsilon)
            {
                // under-damped
                float omegaZeta = angularFrequency * dampingRatio;
                float alpha = angularFrequency * Mathf.Sqrt(1.0f - dampingRatio * dampingRatio);

                float expTerm = Mathf.Exp(-omegaZeta * deltaTime);
                float cosTerm = Mathf.Cos(alpha * deltaTime);
                float sinTerm = Mathf.Sin(alpha * deltaTime);

                float invAlpha = 1.0f / alpha;

                float expSin = expTerm * sinTerm;
                float expCos = expTerm * cosTerm;
                float expOmegaZetaSin_Over_Alpha = expTerm * omegaZeta * sinTerm * invAlpha;

                pOutParams.posPosCoef = expCos + expOmegaZetaSin_Over_Alpha;
                pOutParams.posVelCoef = expSin * invAlpha;

                pOutParams.velPosCoef = -expSin * alpha - omegaZeta * expOmegaZetaSin_Over_Alpha;
                pOutParams.velVelCoef = expCos - expOmegaZetaSin_Over_Alpha;
            }
            else
            {
                // critically damped
                float expTerm = Mathf.Exp(-angularFrequency * deltaTime);
                float timeExp = deltaTime * expTerm;
                float timeExpFreq = timeExp * angularFrequency;

                pOutParams.posPosCoef = timeExpFreq + expTerm;
                pOutParams.posVelCoef = timeExp;

                pOutParams.velPosCoef = -angularFrequency * timeExpFreq;
                pOutParams.velVelCoef = -timeExpFreq + expTerm;
            }
            return pOutParams;
        }

        //******************************************************************************
        // This function will update the supplied position and velocity values over
        // according to the motion parameters.
        //******************************************************************************
        private static void UpdateDampedSpringMotion(
            ref float pPos,           // position value to update
            ref float pVel,           // velocity value to update
            float equilibriumPos, // position to approach
            DampedSpringMotionParams parameters)         // motion parameters to use
        {
            float oldPos = pPos - equilibriumPos; // update in equilibrium relative space
            float oldVel = pVel;

            pPos = oldPos * parameters.posPosCoef + oldVel * parameters.posVelCoef + equilibriumPos;
            pVel = oldPos * parameters.velPosCoef + oldVel * parameters.velVelCoef;
        }

        /// <summary>
        /// Calculate a spring motion development for a given deltaTime
        /// </summary>
        /// <param name="position">"Live" position value</param>
        /// <param name="velocity">"Live" velocity value</param>
        /// <param name="equilibriumPosition">Goal (or rest) position</param>
        /// <param name="deltaTime">Time to update over</param>
        /// <param name="angularFrequency">Angular frequency of motion</param>
        /// <param name="dampingRatio">Damping ratio of motion</param>
        public static void CalcDampedSimpleHarmonicMotion(ref float position, ref float velocity,
            float equilibriumPosition, float deltaTime, float angularFrequency, float dampingRatio)
        {
            var motionParams = CalcDampedSpringMotionParams(deltaTime, angularFrequency, dampingRatio);
            UpdateDampedSpringMotion(ref position, ref velocity, equilibriumPosition, motionParams);
        }

        /// <summary>
        /// Calculate a spring motion development for a given deltaTime
        /// </summary>
        /// <param name="position">"Live" position value</param>
        /// <param name="velocity">"Live" velocity value</param>
        /// <param name="equilibriumPosition">Goal (or rest) position</param>
        /// <param name="deltaTime">Time to update over</param>
        /// <param name="angularFrequency">Angular frequency of motion</param>
        /// <param name="dampingRatio">Damping ratio of motion</param>
        public static void CalcDampedSimpleHarmonicMotion(ref Vector2 position, ref Vector2 velocity,
            Vector2 equilibriumPosition, float deltaTime, float angularFrequency, float dampingRatio)
        {
            var motionParams = CalcDampedSpringMotionParams(deltaTime, angularFrequency, dampingRatio);
            UpdateDampedSpringMotion(ref position.x, ref velocity.x, equilibriumPosition.x, motionParams);
            UpdateDampedSpringMotion(ref position.y, ref velocity.y, equilibriumPosition.y, motionParams);
        }

        /// <summary>
        /// Calculate a spring motion development for a given deltaTime
        /// </summary>
        /// <param name="position">"Live" position value</param>
        /// <param name="velocity">"Live" velocity value</param>
        /// <param name="equilibriumPosition">Goal (or rest) position</param>
        /// <param name="deltaTime">Time to update over</param>
        /// <param name="angularFrequency">Angular frequency of motion</param>
        /// <param name="dampingRatio">Damping ratio of motion</param>
        public static void CalcDampedSimpleHarmonicMotion(ref Vector3 position, ref Vector3 velocity,
            Vector3 equilibriumPosition, float deltaTime, float angularFrequency, float dampingRatio)
        {
            var motionParams = CalcDampedSpringMotionParams(deltaTime, angularFrequency, dampingRatio);
            UpdateDampedSpringMotion(ref position.x, ref velocity.x, equilibriumPosition.x, motionParams);
            UpdateDampedSpringMotion(ref position.y, ref velocity.y, equilibriumPosition.y, motionParams);
            UpdateDampedSpringMotion(ref position.z, ref velocity.z, equilibriumPosition.z, motionParams);
        }

        /// <summary>
        /// Calculate a spring motion development for a given deltaTime quickly without 
        /// considering corner cases for dampingRatio or angularFrequency 
        /// </summary>
        /// <param name="position">"Live" position value</param>
        /// <param name="velocity">"Live" velocity value</param>
        /// <param name="equilibriumPosition">Goal (or rest) position</param>
        /// <param name="deltaTime">Time to update over</param>
        /// <param name="angularFrequency">Angular frequency of motion</param>
        /// <param name="dampingRatio">Damping ratio of motion</param>
        public static void CalcDampedSimpleHarmonicMotionFast(ref float position, ref float velocity,
                float equilibriumPosition, float deltaTime, float angularFrequency, float dampingRatio)
        {
            float x = position - equilibriumPosition;
            velocity += (-dampingRatio * velocity) - (angularFrequency * x);
            position += velocity * deltaTime;
        }

        /// <summary>
        /// Calculate a spring motion development for a given deltaTime quickly without 
        /// considering corner cases for dampingRatio or angularFrequency 
        /// </summary>
        /// <param name="position">"Live" position value</param>
        /// <param name="velocity">"Live" velocity value</param>
        /// <param name="equilibriumPosition">Goal (or rest) position</param>
        /// <param name="deltaTime">Time to update over</param>
        /// <param name="angularFrequency">Angular frequency of motion</param>
        /// <param name="dampingRatio">Damping ratio of motion</param>
        public static void CalcDampedSimpleHarmonicMotionFast(ref Vector2 position, ref Vector2 velocity,
                Vector2 equilibriumPosition, float deltaTime, float angularFrequency, float dampingRatio)
        {
            Vector2 x = position - equilibriumPosition;
            velocity += (-dampingRatio * velocity) - (angularFrequency * x);
            position += velocity * deltaTime;
        }

        /// <summary>
        /// Calculate a spring motion development for a given deltaTime quickly without 
        /// considering corner cases for dampingRatio or angularFrequency 
        /// </summary>
        /// <param name="position">"Live" position value</param>
        /// <param name="velocity">"Live" velocity value</param>
        /// <param name="equilibriumPosition">Goal (or rest) position</param>
        /// <param name="deltaTime">Time to update over</param>
        /// <param name="angularFrequency">Angular frequency of motion</param>
        /// <param name="dampingRatio">Damping ratio of motion</param>
        public static void CalcDampedSimpleHarmonicMotionFast(ref Vector3 position, ref Vector3 velocity,
                Vector3 equilibriumPosition, float deltaTime, float angularFrequency, float dampingRatio)
        {
            Vector3 x = position - equilibriumPosition;
            velocity += (-dampingRatio * velocity) - (angularFrequency * x);
            position += velocity * deltaTime;
        }
    }
}