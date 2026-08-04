import { apiContracts } from '../../domain/api-contracts.js';
import { documentControl, responseEnvelope, contractNamingNotice, successMatrix } from '../../domain/documentation-model.js';
import { purchaseFlow, identifiers } from '../../domain/purchase-flow.js';
import { testScenarios, errorScenarios, limitations, openQuestions } from '../../domain/quality-data.js';

const navigation = Object.freeze([
  {label:'Overview',url:'index.html',page:'home',keywords:'dashboard architecture overview'},
  {label:'Quick Start',url:'pages/quick-start.html',page:'quick-start',keywords:'login catalog initial execute check'},
  {label:'Purchase Flow',url:'pages/purchase-flow.html',page:'purchase-flow',keywords:'workflow identifiers transaction lifecycle'},
  {label:'API Reference',url:'pages/api-reference.html',page:'api-reference',keywords:'endpoints request response headers fields'},
  {label:'Offline Playground',url:'pages/playground.html',page:'playground',keywords:'json builder simulation'},
  {label:'Error Assistant',url:'pages/error-assistant.html',page:'error-assistant',keywords:'timeout token reservation permission'},
  {label:'Test Coverage',url:'pages/test-coverage.html',page:'test-coverage',keywords:'qa tested observed pending'},
  {label:'Platform Architecture',url:'pages/platform-architecture.html',page:'platform-architecture',keywords:'production clean architecture ddd database api users roles admin client'},
  {label:'Governance',url:'pages/governance.html',page:'governance',keywords:'document control revision approval'},
  {label:'Known Limitations',url:'pages/known-limitations.html',page:'known-limitations',keywords:'confirmed observed inference pending'},
  {label:'Open Questions',url:'pages/open-questions.html',page:'open-questions',keywords:'backend confirmation'}
]);

export class StaticDocumentationRepository {
  getDocumentControl() { return documentControl; }
  getApis() { return apiContracts; }
  getApi(ref) { return apiContracts.find((api) => api.ref === ref) ?? null; }
  getResponseEnvelope() { return responseEnvelope; }
  getContractNamingNotice() { return contractNamingNotice; }
  getSuccessMatrix() { return successMatrix; }
  getPurchaseFlow() { return purchaseFlow; }
  getIdentifiers() { return identifiers; }
  getTestScenarios() { return testScenarios; }
  getErrorScenarios() { return errorScenarios; }
  getErrorScenariosArray() { return Object.entries(errorScenarios); }
  getLimitations() { return limitations; }
  getOpenQuestions() { return openQuestions; }
  getNavigation() { return navigation; }
}
