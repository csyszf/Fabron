import * as pulumi from "@pulumi/pulumi";
import { RedisConfig } from "./redis";
import { ConfigMap, Secret, Service, ServiceSpecType } from "@pulumi/kubernetes/core/v1";
import { service_name, namespace_name, shared_labels, app_name_api, image_repo_api, host } from "./core";
import { Deployment } from "@pulumi/kubernetes/apps/v1";
import { Role, RoleBinding } from "@pulumi/kubernetes/rbac/v1";
import { ServiceAccount } from "@pulumi/kubernetes/core/v1";
import { PgSQLConfig } from "./pgsql";
import { Ingress } from "@pulumi/kubernetes/networking/v1beta1";

const config = new pulumi.Config();
const image_version = config.require("IMAGE_VERSION");
const image = `${image_repo_api}:${image_version}`;

export function deploy(redis_config: RedisConfig, pgsql_config: PgSQLConfig) {
    const sa = deploy_rbac();
    const secret = deploy_secret(redis_config, pgsql_config);
    const configmap = deploy_configmap();
    const { deployment, service } = deploy_app(app_name_api, image, configmap, secret, sa);
    deploy_ingress(service);
    return { deployment, service }
}

function deploy_rbac() {
    const service_account = new ServiceAccount(app_name_api, {
        metadata: {
            name: app_name_api,
            namespace: namespace_name
        }
    });
    const subject = {
        kind: service_account.kind,
        name: service_account.metadata.name,
        namespace: service_account.metadata.namespace
    };

    const pod_reader_role = new Role("pod_reader", {
        metadata: {
            name: "pod_reader",
            namespace: subject.namespace
        },
        rules: [{
            apiGroups: [""],
            resources: ["pods"],
            verbs: ["get", "list", "watch"]
        }]
    });
    const pod_reader_rolebinding = new RoleBinding(app_name_api, {
        metadata: { name: app_name_api, namespace: subject.namespace },
        roleRef: {
            apiGroup: "",
            kind: pod_reader_role.kind,
            name: pod_reader_role.metadata.name,
        },
        subjects: [subject]
    });

    return service_account;
}


function deploy_secret(redis_config: RedisConfig, pgsql_config: PgSQLConfig) {
    const secret = new Secret(service_name, {
        metadata: {
            namespace: namespace_name,
            name: service_name,
            labels: shared_labels
        },
        type: "Opaque",
        stringData: {
            "RedisConnectionString": pulumi.interpolate`${redis_config.host}:${redis_config.port},password=${redis_config.password}`,
            "PgSQLConnectionString": pulumi.interpolate`Host=${pgsql_config.host};Port=${pgsql_config.port};Database=fabron;Username=postgres;password=${pgsql_config.password};Maximum Pool Size=300`,
        }
    });
    return secret;
}

function deploy_configmap() {
    const configmap = new ConfigMap(service_name, {
        metadata: {
            namespace: namespace_name,
            name: service_name,
            labels: shared_labels
        },
        data: {
        }
    });
    return configmap;
}

function deploy_app(app_name: string, image_name: string, configmap: ConfigMap, secret: Secret, service_account: ServiceAccount) {
    const labels: { [key: string]: string } = {
        app: app_name,
        "orleans/serviceId": app_name,
        "orleans/clusterId": app_name,
        ...shared_labels
    };
    const deployment = new Deployment(app_name, {
        metadata: {
            name: app_name,
            namespace: namespace_name,
            labels: labels
        },
        spec: {
            replicas: 6,
            selector: {
                matchLabels: labels
            },
            template: {
                metadata: {
                    labels: labels,
                },
                spec: {
                    serviceAccountName: service_account.metadata.name,
                    containers: [{
                        name: app_name,
                        image: image_name,
                        ports: [{
                            name: 'http',
                            containerPort: 80
                        }, {
                            name: 'silo',
                            containerPort: 11111
                        }, {
                            name: 'gateway',
                            containerPort: 30000
                        }],
                        livenessProbe: {
                            httpGet: {
                                path: "/health",
                                port: 80
                            },
                            failureThreshold: 10,
                            timeoutSeconds: 5,
                        },
                        readinessProbe: {
                            httpGet: {
                                path: "/health",
                                port: 80
                            },
                            failureThreshold: 3,
                            timeoutSeconds: 3,
                        },
                        env: [{
                            name: "ASPNETCORE_ENVIRONMENT",
                            value: pulumi.getStack()
                        }, {
                            name: "DOTNET_SHUTDOWNTIMEOUTSECONDS",
                            value: "120"
                        }, {
                            name: "ORLEANS_SERVICE_ID",
                            valueFrom: {
                                fieldRef: {
                                    fieldPath: "metadata.labels['orleans/serviceId']"
                                }
                            }
                        }, {
                            name: "ORLEANS_CLUSTER_ID",
                            valueFrom: {
                                fieldRef: {
                                    fieldPath: "metadata.labels['orleans/clusterId']"
                                }
                            }
                        }, {
                            name: "POD_NAMESPACE",
                            valueFrom: {
                                fieldRef: {
                                    fieldPath: "metadata.namespace"
                                }
                            }
                        }, {
                            name: "POD_NAME",
                            valueFrom: {
                                fieldRef: {
                                    fieldPath: "metadata.name"
                                }
                            }
                        }, {
                            name: "POD_IP",
                            valueFrom: {
                                fieldRef: {
                                    fieldPath: "status.podIP"
                                }
                            }
                        }],
                        envFrom: [{
                            secretRef: { name: secret.metadata.name }
                        }, {
                            configMapRef: { name: configmap.metadata.name }
                        }],
                    }],
                }
            }
        }
    });

    const service = new Service(app_name, {
        metadata: {
            name: app_name,
            namespace: namespace_name,
            labels: {
                app: app_name,
                ...shared_labels
            }
        },
        spec: {
            ports: [{
                name: "http",
                port: 80
            }],
            selector: deployment.spec.template.metadata.labels,
            type: ServiceSpecType.ClusterIP
        }
    });

    return { deployment, service };
}

function deploy_ingress(service: Service) {
    return new Ingress(app_name_api, {
        metadata: {
            name: app_name_api,
            namespace: namespace_name,
            labels: shared_labels,
            annotations: {
                "cert-manager.io/cluster-issuer": "letsencrypt"
            }
        },
        spec: {
            ingressClassName: "nginx",
            tls: [
                {
                    hosts: [host],
                    secretName: `${app_name_api}-tls-secret`
                }
            ],
            rules: [
                {
                    host: host,
                    http: {
                        paths: [
                            {
                                path: "/",
                                backend: {
                                    serviceName: service.metadata.name,
                                    servicePort: "http"
                                }
                            },
                        ],
                    },
                }
            ]
        }
    });
}

