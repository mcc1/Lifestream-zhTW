using ECommons;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace AutoRetainerAPI.Configuration
{
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public class SubmarineUnlockPlan
    {
        public string GUID = Guid.NewGuid().ToString();
        public string Name = string.Empty;
        public List<uint> ExcludedRoutes = new();
        public bool Delete = false;
        public bool UnlockSubs = true;
        public bool EnforceDSSSinglePoint = false;
        public bool EnforcePlan = false;

        public bool ShouldSerializeDelete() => false;

        public void CopyFrom(SubmarineUnlockPlan other)
        {
            this.Name = other.Name;
            this.ExcludedRoutes = other.ExcludedRoutes.JSONClone();
            this.UnlockSubs = other.UnlockSubs;
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
