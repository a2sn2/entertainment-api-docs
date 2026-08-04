export const testScenarios = Object.freeze([
  {scenario:'Valid Login',status:'Tested',area:'Authentication',expected:'Token returned and logical authentication succeeds.',notes:'Validated in the test flow.'},
  {scenario:'Retrieve Entertainment Catalog',status:'Tested',area:'Catalog',expected:'Services, offers, fields, types, and categories are returned.',notes:'Validated in the test flow.'},
  {scenario:'Initialize PUBG 120 Purchase',status:'Tested',area:'Purchase',expected:'Price and resolutionID are returned.',notes:'sellPrice 1140; costPrice 1120.'},
  {scenario:'Execute Purchase',status:'Tested',area:'Purchase',expected:'referenceID and transaction details are returned.',notes:'Validated once.'},
  {scenario:'Check Successful Final State',status:'Tested',area:'Status',expected:'status 1 and failureCode SUCCESS.',notes:'Validated in the test flow.'},
  {scenario:'Expired or Invalid Reservation',status:'Observed',area:'Initialization',expected:'Business failure is returned.',notes:'Observed in operational records.'},
  {scenario:'Offer Permission Failure',status:'Observed',area:'Authorization',expected:'Permission failure is returned.',notes:'Observed in operational records.'},
  {scenario:'Invalid Service Code',status:'Pending',area:'Validation',expected:'Validation failure.',notes:'Pending controlled test and formal error contract.'},
  {scenario:'Invalid Offer Code',status:'Pending',area:'Validation',expected:'Validation failure.',notes:'Pending.'},
  {scenario:'Missing Required Field',status:'Pending',area:'Validation',expected:'Initialization rejected.',notes:'Pending.'},
  {scenario:'Duplicate requestId',status:'Pending',area:'Idempotency',expected:'Pending backend confirmation.',notes:'Do not infer duplicate behavior.'},
  {scenario:'Pending Transaction State',status:'Pending',area:'Status',expected:'Pending backend confirmation.',notes:'Complete status list unavailable.'},
  {scenario:'Provider Timeout',status:'Pending',area:'Provider',expected:'Check State or escalation path.',notes:'Pending controlled test.'},
  {scenario:'Load and Performance Testing',status:'Not Performed',area:'Performance',expected:'Defined service target.',notes:'No load-test evidence available.'}
]);

export const errorScenarios = Object.freeze({
  'Execute timed out':{cause:'The request may have reached the backend, but the client did not receive a definitive response.',dont:'Do not repeat Execute Purchase.',action:'Call Check Purchase State using the same requestId.',api:'ENT-PUR-003',retry:'Retry Check State only if the read request temporarily fails.',escalation:'Escalate with requestId and any referenceID if the state remains unknown.'},
  'Token expired':{cause:'The bearer token is no longer accepted.',dont:'Do not continue protected calls with the expired token.',action:'Authenticate again and replace the stored token.',api:'ENT-AUTH-001',retry:'Retry the original read or safe step after successful authentication.',escalation:'Escalate only if valid credentials cannot obtain a new token.'},
  'resolutionID expired':{cause:'The temporary initialization or reservation is no longer valid.',dont:'Do not reuse the same resolutionID.',action:'Submit a new Initial Purchase and use the newly returned resolutionID.',api:'ENT-PUR-001',retry:'Reinitialize, then execute once.',escalation:'Escalate if fresh identifiers expire unexpectedly.'},
  'Permission denied':{cause:'The user or account is not authorized for the selected offer.',dont:'Do not keep retrying the same request.',action:'Verify account permissions and offer availability.',api:'ENT-PUR-001',retry:'Retry only after authorization is corrected.',escalation:'Escalate to the account or permissions owner.'},
  'Invalid service':{cause:'serviceCode is missing, stale, or not recognized.',dont:'Do not guess a replacement code.',action:'Reload the catalog and select a current serviceCode.',api:'ENT-CAT-001',retry:'Submit a new Initial Purchase with validated catalog data.',escalation:'Escalate if the catalog contains a service that Initial rejects.'},
  'Invalid offer':{cause:'offerCode is invalid or does not belong to the selected service.',dont:'Do not use offerCode as a global identifier.',action:'Reload the catalog and select the offer under its parent service.',api:'ENT-CAT-001',retry:'Reinitialize with the correct service and offer pair.',escalation:'Escalate confirmed catalog-to-purchase inconsistencies.'},
  'Missing required field':{cause:'A service field defined by the catalog was not supplied.',dont:'Do not execute without a successful initialization.',action:'Collect the required field value and submit a new Initial Purchase.',api:'ENT-PUR-001',retry:'Retry initialization after validation.',escalation:'Escalate inconsistent or ambiguous field requirements.'},
  'Unknown transaction state':{cause:'The execution result is incomplete or unavailable.',dont:'Do not create a new requestId and repeat the purchase.',action:'Check the original state using the same requestId.',api:'ENT-PUR-003',retry:'Retry the state check according to the read policy.',escalation:'Escalate with requestId, time, service, offer, and any referenceID.'},
  'Provider failure':{cause:'The external provider could not complete the transaction.',dont:'Do not assume refund or reversal behavior.',action:'Record the final state and follow the approved operational support process.',api:'ENT-PUR-003',retry:'Pending provider-specific and backend-confirmed guidance.',escalation:'Escalate with transaction identifiers and provider result.'}
});

export const limitations = Object.freeze([
  {label:'Confirmed',text:'Non-standard property spellings exist in the current API contract.'},
  {label:'Confirmed',text:'Some request and response properties use different letter casing, including requestId and requestID.'},
  {label:'Confirmed',text:'Historical Postman examples may use a previous catalog response structure.'},
  {label:'Pending',text:'The exact resolutionID lifetime is not formally documented.'},
  {label:'Pending',text:'The complete transaction status list is not formally documented.'},
  {label:'Observed',text:'Some English catalog names may be missing.'},
  {label:'Observed',text:'Some catalog codes and classifications require data-quality review.'},
  {label:'Inference',text:'The current field requirement structure may not fully represent mixed required and optional fields.'}
]);

export const openQuestions = Object.freeze([
  'What is the exact validity period of resolutionID?','Can a resolutionID be used more than once?','What is the official requestId format and maximum length?','Is requestId unique per user, account, channel, or entire system?','What happens when a duplicate requestId is submitted?','What are all supported Check Purchase State values?','Is there a Pending or Processing status?','Is an additional API key required for Check Purchase State?','What is the official meaning of smo?','What are the catalog publication rules?','Can one offer be linked to multiple providers?','Is automatic provider failover supported?','What is the refund or reversal procedure after provider failure?','Which error codes are guaranteed as part of the integration contract?'
]);
