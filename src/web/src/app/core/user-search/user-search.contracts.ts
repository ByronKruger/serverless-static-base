/**
 * Transport shape for `GET /api/users`. It stops here: the rest of the app speaks
 * {@link PersonSummary}.
 */
export interface UserSummaryResponse {
  readonly id: number;
  readonly userName: string;
  /**
   * Omitted entirely for an account registered before names existed: the API drops nulls
   * from its responses, so these arrive as `undefined` rather than `null`.
   */
  readonly firstName?: string;
  readonly lastName?: string;
}
