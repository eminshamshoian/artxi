*** Settings ***
Library           RequestsLibrary
Library           Collections

*** Variables ***
${BASE_URL}       %{AUCTION_SERVICE_BASE_URL=http://localhost:7001}
${SESSION_ALIAS}  auction

*** Keywords ***
Setup Auction API Session
    ${headers}=    Create Dictionary    Content-Type=application/json    Accept=application/json
    Create Session    ${SESSION_ALIAS}    ${BASE_URL}    headers=${headers}
    Set Suite Variable    ${DEFAULT_HEADERS}    ${headers}

Teardown Auction API Session
    Delete All Sessions
