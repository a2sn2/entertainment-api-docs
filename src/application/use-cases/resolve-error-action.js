export function resolveErrorAction(errorScenarios, scenarioName) {
  const scenario = errorScenarios[scenarioName];
  if (!scenario) return null;
  return Object.freeze({ name: scenarioName, ...scenario });
}
