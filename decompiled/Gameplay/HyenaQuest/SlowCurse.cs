using UnityEngine.Scripting;

namespace HyenaQuest;

[Preserve]
[CurseType(CURSE_TYPE.SLOW)]
public class SlowCurse : Curse
{
	public SlowCurse(entity_player owner, bool server, params object[] args)
		: base(owner, server)
	{
	}
}
