*** Settings ***
Documentation     Smoke coverage for Auction Service REST endpoints using Robot Framework.
Resource          resources/Common.resource
Suite Setup       Setup Auction API Session
Suite Teardown    Teardown Auction API Session

*** Variables ***
${ITEM_DESCRIPTION}    Created via Robot Framework test suite.
${MIME_TYPE}           image/png
${ASSET_URL}           https://example.com/assets/robot-item.png

*** Test Cases ***
List Scheduled Auctions
    ${params}=    Create Dictionary    status=Scheduled    includeItem=false    page=1    pageSize=5
    ${response}=    GET On Session    ${SESSION_ALIAS}    url=/api/auctions    params=${params}
    Should Be Equal As Integers    ${response.status_code}    200
    ${auctions}=    Call Method    ${response}    json
    ${count}=    Get Length    ${auctions}
    Should Be True    ${count} >= 0

Create And Delete Item Roundtrip
    ${uuid}=    Evaluate    str(__import__('uuid').uuid4())
    ${title}=    Catenate    SEPARATOR=    Robot Item    ${uuid}
    ${payload}=    Create Dictionary    title=${title}    description=${ITEM_DESCRIPTION}    mimeType=${MIME_TYPE}    assetUrl=${ASSET_URL}

    ${create_response}=    POST On Session    ${SESSION_ALIAS}    url=/api/items    json=${payload}
    ${status}=    Set Variable    ${create_response.status_code}
    Run Keyword If    ${status} != 201    Log    ${create_response}
    Should Be Equal As Integers    ${status}    201
    ${created}=    Call Method    ${create_response}    json
    Dictionary Should Contain Key    ${created}    id
    ${item_id}=    Get From Dictionary    ${created}    id
    Should Not Be Empty    ${item_id}

    ${get_response}=    GET On Session    ${SESSION_ALIAS}    url=/api/items/${item_id}
    Should Be Equal As Integers    ${get_response.status_code}    200

    ${delete_response}=    DELETE On Session    ${SESSION_ALIAS}    url=/api/items/${item_id}
    Should Be Equal As Integers    ${delete_response.status_code}    204
