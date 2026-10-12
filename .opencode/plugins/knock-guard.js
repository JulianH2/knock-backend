// GENERADO por scripts/sync_agents.py desde scripts/sync_agents.py. No editar a mano.
// Guardián de Knock para opencode: delega en scripts/hooks/knock_guard.py.
import { spawnSync } from "node:child_process";
import path from "node:path";

const guard = (dir, event, payload) =>
  spawnSync(process.env.KNOCK_PYTHON || "python3", [path.join(dir, "scripts/hooks/knock_guard.py"), event, "--tool", "opencode"], {
    input: JSON.stringify(payload), encoding: "utf8", cwd: dir,
  });

export const KnockGuard = async ({ directory, worktree }) => {
  const dir = worktree || directory;
  return {
    "tool.execute.before": async (input, output) => {
      const r = guard(dir, "pre-tool", { tool_name: input.tool, tool_input: output.args });
      if (r.status === 2) throw new Error(r.stderr || r.stdout);
    },
    event: async ({ event }) => {
      if (event.type === "session.idle") {
        const r = guard(dir, "stop", { session_id: event.properties?.sessionID });
        if (r.status === 2) console.error(r.stderr);
      }
    },
  };
};
