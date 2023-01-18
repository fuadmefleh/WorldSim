using System;
using System.Collections.Generic;
using System.Text;

namespace WorldSimLib.Utils
{
    class StaticRandom
    {
        private static Random instance;

        #region Properties

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <value>The instance.</value>
        public static Random Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new Random();
                }
                return instance;
            }
        }

        #endregion
    }
}
