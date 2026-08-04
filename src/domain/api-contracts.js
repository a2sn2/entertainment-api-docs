const headers = {
  public: [
    { field:'Content-Type', type:'Header', required:'Yes', description:'Request content type.', example:'application/json' },
    { field:'Channel', type:'Header', required:'Yes', description:'Client channel identifier.', example:'CLSChAPi' }
  ],
  bearer: [
    { field:'Content-Type', type:'Header', required:'Yes', description:'Request content type.', example:'application/json' },
    { field:'Authorization', type:'Header', required:'Yes', description:'Bearer access token.', example:'Bearer {{token}}' }
  ]
};

export const apiContracts = Object.freeze([
  {
    ref:'ENT-AUTH-001', slug:'login', name:'Login', group:'Authentication', method:'POST', path:'/api/v1/acs/Auth/login_un_enc', auth:'Not required', status:'Tested',
    purpose:'Authenticate a user and obtain an access token for protected APIs.', headers:headers.public,
    requestFields:[
      {field:'theType',type:'String',required:'According to current contract',description:'Operation wrapper value.',example:'string'},
      {field:'request',type:'Object',required:'Yes',description:'Authentication request object.',example:'—'},
      {field:'UserName',type:'String',required:'Yes',description:'User login name.',example:'{{username}}'},
      {field:'Password',type:'String',required:'Yes',description:'User password.',example:'{{password}}'},
      {field:'version',type:'String',required:'Yes',description:'Client or application version.',example:'4.0.1.0'},
      {field:'messagesRequests',type:'Array',required:'According to current contract',description:'Current wrapper message structure.',example:'—'}
    ],
    responseFields:[
      {field:'isAuthenticated',type:'Boolean',description:'Indicates whether authentication succeeded.',example:'true'},
      {field:'username',type:'String',description:'Authenticated username.',example:'ApiUser'},
      {field:'token',type:'String',description:'Access token used by protected APIs.',example:'{{token}}'},
      {field:'refreshToken',type:'String',description:'Session renewal token when supported.',example:'{{refreshToken}}'},
      {field:'expiresOn',type:'DateTime',description:'Access-token expiration date and time.',example:'2026-08-04T17:17:23Z'},
      {field:'userId',type:'Integer or String',description:'Authenticated user identifier.',example:'105'},
      {field:'roles',type:'Array',description:'Assigned user roles.',example:'["Admin"]'}
    ],
    requestExample:{theType:'string',request:{UserName:'{{username}}',Password:'{{password}}'},version:'4.0.1.0',messagesRequests:[{faild:'string',flag:true}]},
    responseExample:{result:{isAuthenticated:true,username:'ApiUser',token:'{{token}}',refreshToken:'{{refreshToken}}',expiresOn:'2026-08-04T17:17:23Z',userId:105,roles:['Admin']},code:1000,massage:'Success',statues:true,messages:null,errors:null},
    success:['isAuthenticated = true','token is not null or empty'],
    rules:['Do not store credentials in shared collections or source code.','Do not expose access or refresh tokens in screenshots or documentation.','Clear stale tokens after failed authentication.','Use separate test and production accounts.'],
    errors:['Correct invalid credentials and authenticate again.','Do not treat HTTP 200 as success without validating the logical authentication result.','Expired-token behavior is pending full backend documentation.']
  },
  {
    ref:'ENT-CAT-001', slug:'catalog', name:'Get All Entertainment Data', group:'Catalog', method:'POST', path:'/api/v1/acs/ops/prs_un_enc', auth:'Bearer Token', status:'Tested',
    purpose:'Retrieve service types, categories, field definitions, services, and offers.', headers:headers.bearer,
    requestFields:[
      {field:'theType',type:'String',required:'Yes',description:'Backend operation identifier.',example:'UtilitesAsync_GetAllEntertainmentDataServices.GetAllEntertainmentData'},
      {field:'request',type:'Object',required:'Yes',description:'Empty request object.',example:'{}'}
    ],
    responseFields:[
      {field:'services',type:'Array',description:'Entertainment services and their offers.',example:'[]'},
      {field:'types',type:'Array',description:'Main entertainment service types.',example:'[]'},
      {field:'fields',type:'Array',description:'Available field definitions.',example:'[]'},
      {field:'categories',type:'Array',description:'Offer categories or regions.',example:'[]'},
      {field:'serviceCode',type:'String',description:'Service code used in purchase requests.',example:'pubg'},
      {field:'serviceName',type:'String',description:'Service display name.',example:'بوبجي العالميه'},
      {field:'serviceNameEN',type:'String or Null',description:'English service name.',example:'null'},
      {field:'smo',type:'Integer',description:'Current service ordering value; official meaning pending confirmation.',example:'1'},
      {field:'typeID',type:'Integer',description:'Service type identifier.',example:'1'},
      {field:'fieldID',type:'Array',description:'Required field identifiers.',example:'[1]'},
      {field:'fieldRequired',type:'Boolean',description:'Service-level field requirement indicator.',example:'true'},
      {field:'offerCode',type:'String',description:'Offer code used in purchase requests.',example:'120'},
      {field:'offerName',type:'String',description:'Offer display name.',example:'ببجي 120 شده'},
      {field:'order',type:'Integer',description:'Offer display order.',example:'2'},
      {field:'categoryID',type:'Integer',description:'Offer category identifier.',example:'1'}
    ],
    requestExample:{theType:'UtilitesAsync_GetAllEntertainmentDataServices.GetAllEntertainmentData',request:{}},
    responseExample:{result:{services:[{serviceName:'بوبجي العالميه',serviceNameEN:null,serviceCode:'pubg',smo:1,typeID:1,fieldID:[1],fieldRequired:true,offers:[{offerName:'ببجي 120 شده',offerNameEN:null,offerCode:'120',order:2,categoryID:1}]}],types:[],fields:[],categories:[]},code:1000,massage:'Success',statues:true,messages:null,errors:null},
    success:['HTTP 200','code = 1000','statues = true','result is not null'],
    rules:['Retrieve serviceCode, offerCode, and fieldID from the current catalog.','An offer must belong to the selected service.','offerCode is not globally unique.','The final selling price comes from Initial Purchase.','Catalog caching requires an approved refresh policy.'],
    errors:['Read-only catalog failures may be retried according to the client read policy.','Historical Postman examples may use an older response structure.','Some catalog names, codes, and classifications require data-quality review.']
  },
  {
    ref:'ENT-PUR-001', slug:'initial-purchase', name:'Initial Purchase', group:'Purchase', method:'POST', path:'/api/v1/acs/ops/prs_un_enc', auth:'Bearer Token', status:'Tested',
    purpose:'Validate the purchase, determine the provider and price, and create a temporary initialization identifier.', headers:headers.bearer,
    requestFields:[
      {field:'theType',type:'String',required:'Yes',description:'Backend operation identifier.',example:'UtilitesAsync_InitialPurchaseServices.InitialPurchase'},
      {field:'request',type:'Object',required:'Yes',description:'Purchase initialization object.',example:'—'},
      {field:'serviceCode',type:'String',required:'Yes',description:'Service code from the current catalog.',example:'pubg'},
      {field:'offerCode',type:'String',required:'Yes',description:'Offer code under the selected service.',example:'120'},
      {field:'feilds',type:'Array',required:'Yes',description:'Required service fields using the current contract spelling.',example:'—'},
      {field:'fieldID',type:'String',required:'Yes',description:'Field identifier from the catalog.',example:'1'},
      {field:'value',type:'String',required:'Yes',description:'Customer or player value.',example:'PLAYER_ID_SAMPLE'}
    ],
    responseFields:[
      {field:'providerName',type:'String',description:'Selected provider name.',example:'Sample Provider'},
      {field:'providerCode',type:'String',description:'Selected provider code.',example:'500001'},
      {field:'serviceName',type:'String',description:'Selected service name.',example:'بوبجي العالميه'},
      {field:'serviceCode',type:'String',description:'Selected service code.',example:'pubg'},
      {field:'typeName',type:'String',description:'Service type name.',example:'العاب'},
      {field:'feildName',type:'String',description:'Name of the submitted field.',example:'رقم اللاعب'},
      {field:'offerName',type:'String',description:'Selected offer name.',example:'ببجي 120 شده'},
      {field:'offerNameEN',type:'String or Null',description:'English offer name.',example:'null'},
      {field:'offerCode',type:'String',description:'Selected offer code.',example:'120'},
      {field:'sellPrice',type:'Decimal',description:'Final selling price before execution.',example:'1140.0000'},
      {field:'costPrice',type:'Decimal',description:'Internal purchase cost.',example:'1120.0000'},
      {field:'categoryName',type:'String',description:'Offer category.',example:'عالمي'},
      {field:'resolutionID',type:'String or UUID',description:'Temporary initialization identifier.',example:'SAMPLE-RESOLUTION-ID'}
    ],
    requestExample:{theType:'UtilitesAsync_InitialPurchaseServices.InitialPurchase',request:{serviceCode:'pubg',offerCode:'120',feilds:[{fieldID:'1',value:'PLAYER_ID_SAMPLE'}]}},
    responseExample:{result:{providerName:'Sample Provider',providerCode:'500001',serviceName:'بوبجي العالميه',serviceCode:'pubg',typeName:'العاب',feildName:'رقم اللاعب',offerName:'ببجي 120 شده',offerNameEN:null,offerCode:'120',sellPrice:1140,costPrice:1120,categoryName:'عالمي',resolutionID:'SAMPLE-RESOLUTION-ID'},code:1000,massage:'Success',statues:true,messages:null,errors:null},
    success:['HTTP 200','code = 1000','statues = true','resolutionID is not empty'],
    rules:['Initial Purchase must succeed before Execute Purchase.','Use the returned resolutionID.','resolutionID is temporary and may expire.','Use sellPrice as the final customer price.','costPrice is internal and must not be shown to end users.','The validated PUBG 120 flow required Player ID only.'],
    errors:['Correct invalid input and submit a new initialization.','If resolutionID expires, initialize again and do not reuse the previous value.','The exact resolutionID lifetime is pending backend confirmation.']
  },
  {
    ref:'ENT-PUR-002', slug:'execute-purchase', name:'Execute Purchase', group:'Purchase', method:'POST', path:'/api/v1/acs/ops/prs_un_enc', auth:'Bearer Token', status:'Tested',
    purpose:'Execute a previously initialized entertainment purchase exactly once.', headers:headers.bearer,
    requestFields:[
      {field:'theType',type:'String',required:'Yes',description:'Backend operation identifier.',example:'UtilitesAsync_ExecutePurchaseServices.ExecutePurchase'},
      {field:'request',type:'Object',required:'Yes',description:'Purchase execution object.',example:'—'},
      {field:'resolutionID',type:'String or UUID',required:'Yes',description:'Identifier returned by Initial Purchase.',example:'SAMPLE-RESOLUTION-ID'},
      {field:'requestId',type:'String',required:'Yes',description:'Client-generated unique transaction identifier.',example:'REQ-SAMPLE-0001'}
    ],
    responseFields:[
      {field:'referenceID',type:'String',description:'Backend transaction reference.',example:'260803202216014'},
      {field:'requestID',type:'String',description:'Returned client request identifier.',example:'REQ-SAMPLE-0001'},
      {field:'playerID',type:'String',description:'Submitted customer or player identifier.',example:'PLAYER_ID_SAMPLE'},
      {field:'playerName',type:'String or Null',description:'Player name when available.',example:'null'},
      {field:'serviceName',type:'String',description:'Purchased service name.',example:'بوبجي العالميه'},
      {field:'serviceCode',type:'String',description:'Purchased service code.',example:'pubg'},
      {field:'offerName',type:'String',description:'Purchased offer name.',example:'ببجي 120 شده'},
      {field:'offerCode',type:'String',description:'Purchased offer code.',example:'120'},
      {field:'amount',type:'Decimal',description:'Executed transaction amount.',example:'1140.0000'},
      {field:'processedAt',type:'DateTime',description:'Processing timestamp.',example:'2026-08-03T20:22:16+03:00'},
      {field:'message',type:'String',description:'Processing result message.',example:'Purchase processed successfully'}
    ],
    requestExample:{theType:'UtilitesAsync_ExecutePurchaseServices.ExecutePurchase',request:{resolutionID:'SAMPLE-RESOLUTION-ID',requestId:'REQ-SAMPLE-0001'}},
    responseExample:{result:{referenceID:'260803202216014',requestID:'REQ-SAMPLE-0001',playerID:'PLAYER_ID_SAMPLE',playerName:null,serviceName:'بوبجي العالميه',serviceCode:'pubg',offerName:'ببجي 120 شده',offerCode:'120',amount:1140,processedAt:'2026-08-03T20:22:16+03:00',message:'Purchase processed successfully'},code:1000,massage:'Success',statues:true,messages:null,errors:null},
    success:['HTTP 200','code = 1000','statues = true','transaction identifiers are returned','final success still requires Check Purchase State'],
    rules:['Generate and store a unique requestId before sending.','Store the returned referenceID.','Verify amount matches the initialized selling price.','Submit Execute Purchase once only.','Do not create a new requestId to retry an uncertain transaction before checking the original state.'],
    errors:['On timeout or network interruption, do not repeat Execute Purchase.','Call ENT-PUR-003 using the same requestId.','Duplicate requestId behavior is pending backend confirmation.']
  },
  {
    ref:'ENT-PUR-003', slug:'check-state', name:'Check Purchase State', group:'Purchase', method:'POST', path:'/api/v1/acs/ops/prs_un_enc', auth:'Bearer Token', status:'Tested',
    purpose:'Retrieve the current or final purchase transaction state using the original client request identifier.', headers:headers.bearer,
    requestFields:[
      {field:'theType',type:'String',required:'Yes',description:'Backend operation identifier.',example:'UtilitesAsync_ExecutePurchaseServices.CheckState'},
      {field:'request',type:'Object',required:'Yes',description:'State-check request object.',example:'—'},
      {field:'requestId',type:'String',required:'Yes',description:'Same identifier submitted to Execute Purchase.',example:'REQ-SAMPLE-0001'}
    ],
    responseFields:[
      {field:'referenceID',type:'String',description:'Backend transaction reference.',example:'260803202216014'},
      {field:'requestID',type:'String',description:'Original client transaction identifier.',example:'REQ-SAMPLE-0001'},
      {field:'playerID',type:'String',description:'Player or customer identifier.',example:'PLAYER_ID_SAMPLE'},
      {field:'providerName',type:'String',description:'Provider that processed the transaction.',example:'Sample Provider'},
      {field:'providerCode',type:'String',description:'Provider code.',example:'500001'},
      {field:'serviceCode',type:'String',description:'Service code.',example:'pubg'},
      {field:'offerCode',type:'String',description:'Offer code.',example:'120'},
      {field:'amount',type:'Decimal',description:'Transaction amount.',example:'1140.0000'},
      {field:'status',type:'Integer',description:'Numeric transaction status.',example:'1'},
      {field:'statusName',type:'String',description:'Human-readable transaction status.',example:'ناجحة'},
      {field:'failureCode',type:'String',description:'Success or failure code.',example:'SUCCESS'},
      {field:'processedAt',type:'DateTime',description:'Transaction processing timestamp.',example:'2026-08-03T20:22:16'},
      {field:'message',type:'String',description:'Final transaction message.',example:'Transaction completed successfully'}
    ],
    requestExample:{theType:'UtilitesAsync_ExecutePurchaseServices.CheckState',request:{requestId:'REQ-SAMPLE-0001'}},
    responseExample:{result:{referenceID:'260803202216014',requestID:'REQ-SAMPLE-0001',playerID:'PLAYER_ID_SAMPLE',playerName:'',providerName:'Sample Provider',providerCode:'500001',serviceName:'بوبجي العالميه',serviceCode:'pubg',offerName:'ببجي 120 شده',offerCode:'120',amount:1140,status:1,statusName:'ناجحة',failureCode:'SUCCESS',processedAt:'2026-08-03T20:22:16',message:'Transaction completed successfully'},code:1000,massage:'Success',statues:true,messages:null,errors:null},
    success:['status = 1','statusName = ناجحة','failureCode = SUCCESS'],
    rules:['Verify requestID, referenceID, playerID, serviceCode, offerCode, and amount against the original transaction.','Use the same requestId submitted to Execute Purchase.'],
    errors:['Retry Check Purchase State with the same requestId after temporary read failures.','The complete status list and pending-state behavior are pending backend confirmation.','An additional API key requirement is not formally confirmed.']
  }
]);
