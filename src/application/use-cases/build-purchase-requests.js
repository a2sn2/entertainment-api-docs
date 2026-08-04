export function buildPurchaseRequests(values) {
  const clean = Object.fromEntries(Object.entries(values).map(([key, value]) => [key, String(value ?? '').trim()]));
  return Object.freeze({
    initial: {
      theType: 'UtilitesAsync_InitialPurchaseServices.InitialPurchase',
      request: {
        serviceCode: clean.serviceCode,
        offerCode: clean.offerCode,
        feilds: [{ fieldID: clean.fieldID, value: clean.value }]
      }
    },
    execute: {
      theType: 'UtilitesAsync_ExecutePurchaseServices.ExecutePurchase',
      request: { resolutionID: clean.resolutionID, requestId: clean.requestId }
    },
    check: {
      theType: 'UtilitesAsync_ExecutePurchaseServices.CheckState',
      request: { requestId: clean.requestId }
    }
  });
}

export function validatePurchaseInputs(values, mode) {
  const requiredByMode = {
    initial: ['serviceCode', 'offerCode', 'fieldID', 'value'],
    execute: ['resolutionID', 'requestId'],
    check: ['requestId']
  };
  const missing = (requiredByMode[mode] ?? []).filter((key) => !String(values[key] ?? '').trim());
  return Object.freeze({ valid: missing.length === 0, missing });
}
