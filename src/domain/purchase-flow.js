export const purchaseFlow = Object.freeze([
  {id:'login',order:1,title:'Login',api:'ENT-AUTH-001',input:'Username, password, version, and Channel header.',output:'Bearer token.',pass:'token',note:'Validate isAuthenticated and a non-empty token.'},
  {id:'catalog',order:2,title:'Catalog',api:'ENT-CAT-001',input:'Bearer token and empty request object.',output:'Current services, offers, fields, types, and categories.',pass:'serviceCode + offerCode + fieldID',note:'Do not rely only on hard-coded catalog values.'},
  {id:'initial',order:3,title:'Initial Purchase',api:'ENT-PUR-001',input:'serviceCode, offerCode, and required field values.',output:'sellPrice, costPrice, and resolutionID.',pass:'sellPrice + resolutionID',note:'resolutionID is temporary and may expire.'},
  {id:'execute',order:4,title:'Execute Purchase',api:'ENT-PUR-002',input:'Current resolutionID and unique requestId.',output:'requestID, referenceID, and amount.',pass:'requestID + referenceID',note:'Submit once only. On uncertainty, do not repeat Execute.'},
  {id:'check',order:5,title:'Check Purchase State',api:'ENT-PUR-003',input:'The same requestId used for Execute.',output:'status, statusName, and failureCode.',pass:'Final state',note:'Correlate all identifiers and the amount before closing the transaction.'}
]);

export const identifiers = Object.freeze([
  {name:'resolutionID',owner:'Backend',created:'Initial Purchase',purpose:'Temporary initialization or reservation used by Execute Purchase.',rules:['Temporary','May expire','Do not reuse after expiration']},
  {name:'requestId',owner:'Client',created:'Before Execute Purchase',purpose:'Unique client transaction identifier reused by Check Purchase State.',rules:['Generate before execution','Persist before sending','Reuse for state checks']},
  {name:'referenceID',owner:'Backend',created:'Execute Purchase',purpose:'Backend transaction reference used for support and reconciliation.',rules:['Store after execution','Correlate with requestId','Include in escalation']}
]);
