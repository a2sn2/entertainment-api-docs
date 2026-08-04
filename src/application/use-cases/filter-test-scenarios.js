export function filterTestScenarios(scenarios, status = 'All') {
  if (status === 'All') return [...scenarios];
  return scenarios.filter((scenario) => scenario.status === status);
}
