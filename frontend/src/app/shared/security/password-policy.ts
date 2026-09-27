export const PASSWORD_MIN_LENGTH = 12;
export const PASSWORD_MAX_LENGTH = 128;

export function getPasswordStrengthError(password: string): string | null {
  if (password.length < PASSWORD_MIN_LENGTH) {
    return `A password deve ter pelo menos ${PASSWORD_MIN_LENGTH} caracteres.`;
  }

  if (password.length > PASSWORD_MAX_LENGTH) {
    return `A password não pode ter mais de ${PASSWORD_MAX_LENGTH} caracteres.`;
  }

  if (!/\p{Ll}/u.test(password)) {
    return 'A password deve incluir pelo menos uma letra minúscula.';
  }

  if (!/\p{Lu}/u.test(password)) {
    return 'A password deve incluir pelo menos uma letra maiúscula.';
  }

  if (!/\p{Nd}/u.test(password)) {
    return 'A password deve incluir pelo menos um número.';
  }

  if (!/[^\p{L}\p{N}]/u.test(password)) {
    return 'A password deve incluir pelo menos um símbolo.';
  }

  return null;
}
