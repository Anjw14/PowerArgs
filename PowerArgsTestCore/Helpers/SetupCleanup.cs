﻿using PowerArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArgsTests
{
    public static class SetupCleanup
    {
        public static void ClearAttributeCache()
        {
            MemberInfoEx.CachedAttributes.Clear();
        }
    }
}
