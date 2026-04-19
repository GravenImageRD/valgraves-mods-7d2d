using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Valgraves.Common;

namespace TeamColors
{
    public static class TC
    {
        public static Color? GetPartyColorByEntityId(int entityId)
        {
            if (Player.Entity == null) return null;
            
            if (Player.Entity.Party == null)
            {
                Logging.Error("Player has no party");
                return null;
            }
            
            if (!Player.Entity.Party.ContainsMember(entityId))
            {
                Logging.Error($"Player party does not contain EntityId {entityId}");
                return null;
            }
            
            EntityPlayer partyMember = Player.Entity.Party.MemberList.FirstOrDefault(x => x.entityId == entityId);
            int partyMemberId = Player.Entity.Party.MemberList.IndexOf(partyMember);
            if (partyMemberId  == -1)
            {
                Logging.Error($"Could not find party member index for EntityId {entityId}");
                return null;
            }
                
            return Constants.TrackedFriendColors[partyMemberId];
        }
    }
}