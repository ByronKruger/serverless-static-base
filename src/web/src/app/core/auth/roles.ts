/**
 * Role names, mirroring `src/api/Authorization/Roles.cs`. The UI hides what a role
 * cannot do; the API is what actually enforces it.
 */
export const Roles = {
  Administrator: 'Administrator',
  Moderator: 'Moderator',
  User: 'User',
} as const;

export type Role = (typeof Roles)[keyof typeof Roles];
