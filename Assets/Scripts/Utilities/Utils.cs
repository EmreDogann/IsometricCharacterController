using UnityEngine;

namespace Utilities
{
    public static class Utils
    {
        private static Matrix4x4 _isoMatrix = Matrix4x4.Rotate(Quaternion.Euler(0, 45, 0));

        public static Vector3 toIso(this Vector3 input) => _isoMatrix.MultiplyPoint3x4(input);

        // From: https://twitter.com/Angeline_Gd/status/1285580260935901190
        public static void DrawGizmoCircle(Vector3 center, float radius, Color color, int segments)
        {
            Gizmos.color = color;
            const float TWO_PI = Mathf.PI * 2;
            float step = TWO_PI / (float)segments;
            float theta = 0;
            float x = radius * Mathf.Cos(theta);
            float y = radius * Mathf.Sin(theta);
            Vector3 pos = center + new Vector3(x, 0, y);
            Vector3 newPos;
            Vector3 lastPos = pos;

            for (theta = step; theta < TWO_PI; theta += step)
            {
                x = radius * Mathf.Cos(theta);
                y = radius * Mathf.Sin(theta);
                newPos = center + new Vector3(x, 0, y);
                Gizmos.DrawLine(pos, newPos);
                pos = newPos;
            }
            Gizmos.DrawLine(pos, lastPos);
        }

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
        //******************************************************************************
        // Cached set of motion parameters that can be used to efficiently update
        // multiple springs using the same time step, angular frequency and damping
        // ratio.
        //******************************************************************************
    }
}