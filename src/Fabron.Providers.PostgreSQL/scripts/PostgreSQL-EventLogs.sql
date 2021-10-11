CREATE TABLE fabron_job_eventlogs
(
    entity_key varchar(150) NOT NULL,
    version integer NOT NULL,
    timestamp timestamp(3) NOT NULL,
    type varchar(150) NOT NULL,
    data text NOT NULL,

    CONSTRAINT pk_entitykey_version PRIMARY KEY(entity_key, Version)
);

CREATE TABLE fabron_cronjob_eventlogs
(
    entity_key varchar(150) NOT NULL,
    version integer NOT NULL,
    timestamp timestamp(3) NOT NULL,
    type varchar(150) NOT NULL,
    data text NOT NULL,

    CONSTRAINT pk_entitykey_version PRIMARY KEY(entity_key, Version)
);


CREATE TABLE fabron_job_consumers
(
    entity_key varchar(150) NOT NULL,
    _offset integer NOT NULL,

    CONSTRAINT pk_entitykey PRIMARY KEY(entity_key)
);

CREATE TABLE fabron_cronjob_consumers
(
    entity_key varchar(150) NOT NULL,
    _offset integer NOT NULL,

    CONSTRAINT pk_entitykey PRIMARY KEY(entity_key)
);
