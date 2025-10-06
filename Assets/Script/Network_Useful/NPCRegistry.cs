using System.Collections.Generic;

public static class NPCRegistry
{
    private static readonly HashSet<NPCBehavior> npcs = new HashSet<NPCBehavior>();

    public static void Register(NPCBehavior npc) => npcs.Add(npc);
    public static void Unregister(NPCBehavior npc) => npcs.Remove(npc);

    public static IEnumerable<NPCBehavior> AllNPCsSnapshot() => npcs;
}
