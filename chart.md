flowchart TD
    start(["User taps Deposit<br/>(Beans App)"]) --> buildTx

    buildTx["App builds Stellar transaction:<br/>Payment / PathPaymentStrictSend /<br/>PathPaymentStrictReceive"]
    buildTx --> signTx["App signs transaction<br/>(client-side, user's key)"]
    signTx --> apiCall

    apiCall["POST /v4/me/cards/{cardId}/deposits<br/>Header: Idempotency-Key<br/>Body: amount + signedTransactionXdr"]

    apiCall --> idempotency{"Existing deposit<br/>for idempotency key?"}
    idempotency -- "Yes" --> returnExisting(["Return existing<br/>card transaction"])
    idempotency -- "No" --> validate

    validate{"Validate:<br/>card ownership,<br/>amount > 0,<br/>XDR parseable,<br/>signature threshold"}
    validate -- "Fails" --> errorResponse(["400 / 404<br/>Error response"])
    validate -- "Passes" --> atomicWrite

    atomicWrite["Atomic DB Write:<br/>1. card_deposits (Draft)<br/>2. card_transactions (Draft)<br/>3. Outbox job (2 steps)"]
    atomicWrite --> apiResponse(["200 OK<br/>Returns card transaction"])
    atomicWrite --> step0

    %% ── OUTBOX STEP 0 ──────────────────────────────────────
    step0["Outbox Step 0:<br/>SubmitTransactionToStellarStepRunHandler"]
    step0 --> feeBump["horizonService.FeeBumpTransaction:<br/>Wrap in fee bump envelope<br/>(Beans sponsors fees)<br/>Sign + submit to Stellar"]
    feeBump --> step0Result{"Stellar submission<br/>result?"}

    step0Result -- "Success<br/>(tx hash returned)" --> step1
    step0Result -- "FeeBumpInnerTransaction<br/>FailedException" --> immediateFailure["Immediate failure<br/>(no retries, deterministic)"]
    step0Result -- "Other exception<br/>(network timeout)" --> retry{"Retry count<br/>< 3?"}
    retry -- "Yes" --> step0
    retry -- "No (exhausted)" --> failedHandler

    immediateFailure --> failedHandler

    %% ── OUTBOX STEP 1 ──────────────────────────────────────
    step1["Outbox Step 1:<br/>SyncDatabaseCardDepositRecordsStepRunHandler"]
    step1 --> syncDb["Update deposit:<br/>StellarTransactionHash + Status → Pending<br/>Update card_transaction:<br/>Status → Pending"]
    syncDb --> awaitWebhook(["Deposit now Pending<br/>Awaiting Kulipa webhook"])

    %% ── PERMANENT FAILURE ──────────────────────────────────
    failedHandler["CreateCardDepositJobRunFailedHandler"]
    failedHandler --> markFailed["Deposit status → Failed<br/>Transaction status → Failed"]
    markFailed --> notifyFailed["Push notification:<br/>deposit failed"]
    notifyFailed --> terminalFailed(["Terminal: Failed"])

    %% ── WEBHOOK: CONFIRMED ─────────────────────────────────
    awaitWebhook --> webhookConfirmed

    webhookConfirmed["Webhook received:<br/>blockchainTransaction.confirmed<br/>(type: Deposit)"]
    webhookConfirmed --> parseConfirmed["BlockchainTransactionConfirmedEventProcessor<br/>Parse → BlockchainTransactionConfirmedEventData"]
    parseConfirmed --> lookupUser["Find AnchorUser by ExternalId<br/>Fetch + update card balance"]
    lookupUser --> matchDeposit{"Match deposit by<br/>StellarTransactionHash?"}

    matchDeposit -- "Matched<br/>(in-app deposit)" --> guardConfirm{"Current<br/>deposit status?"}
    matchDeposit -- "Not matched<br/>(external deposit)" --> externalDeposit

    guardConfirm -- "Confirmed" --> skipConfirm(["Skip:<br/>already confirmed"])
    guardConfirm -- "Failed" --> skipTerminal(["Skip + log warning:<br/>do not transition Failed back"])
    guardConfirm -- "Draft / Pending" --> confirmUpdate["Update deposit:<br/>ExternalId + ConfirmedAmount<br/>Status → Confirmed<br/>Update card_transaction:<br/>Amount → confirmed amount<br/>Status → Confirmed"]
    confirmUpdate --> notifyConfirmed["Push notification:<br/>deposit confirmed"]
    notifyConfirmed --> terminalConfirmed(["Terminal: Confirmed"])

    %% ── EXTERNAL DEPOSIT ────────────────────────────────────
    externalDeposit["Create records on-the-fly:<br/>card_deposits (Confirmed)<br/>Amount = ConfirmedAmount = on-chain amount<br/>IdempotencyKey = tx hash<br/>card_transactions (Confirmed)"]
    externalDeposit --> notifyExternal["Push notification:<br/>deposit confirmed"]
    notifyExternal --> terminalExternal(["Terminal: Confirmed<br/>(external deposit)"])

    %% ── STYLES ─────────────────────────────────────────────
    style returnExisting fill:#e8f5e9,stroke:#4caf50,color:#2e7d32
    style apiResponse fill:#e8f5e9,stroke:#4caf50,color:#2e7d32
    style errorResponse fill:#ffebee,stroke:#ef5350,color:#c62828
    style terminalFailed fill:#ffebee,stroke:#ef5350,color:#c62828
    style terminalConfirmed fill:#e8f5e9,stroke:#4caf50,color:#2e7d32
    style terminalExternal fill:#e8f5e9,stroke:#4caf50,color:#2e7d32
    style skipConfirm fill:#f5f5f5,stroke:#999,color:#666
    style skipTerminal fill:#f5f5f5,stroke:#999,color:#666
    style awaitWebhook fill:#e3f2fd,stroke:#42a5f5,color:#1565c0
    style immediateFailure fill:#ffebee,stroke:#ef5350,color:#c62828