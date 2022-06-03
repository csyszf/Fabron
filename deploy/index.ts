import { Deployment } from "@pulumi/kubernetes/apps/v1";
import { Namespace, Secret, Service, ServiceAccount, ServiceSpecType } from "@pulumi/kubernetes/core/v1";
// import { Ingress } from "@pulumi/kubernetes/networking/v1";
import { Ingress } from "@pulumi/kubernetes/networking/v1beta1";
import { Role, RoleBinding } from "@pulumi/kubernetes/rbac/v1";
import { Config, getStack } from "@pulumi/pulumi";

const config = new Config();

const appName = "fabron";

const commonLabels = {
    "app.kubernetes.io/name": appName,
}

const namespace = new Namespace("dca", {
    metadata: {
        name: "dca"
    }
})

const service_account = new ServiceAccount(appName, {
    metadata: {
        name: appName,
        namespace: namespace.metadata.name
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

const pod_reader_rolebinding = new RoleBinding(appName, {
    metadata: { name: appName, namespace: subject.namespace },
    roleRef: {
        apiGroup: "",
        kind: pod_reader_role.kind,
        name: pod_reader_role.metadata.name,
    },
    subjects: [subject]
});

const secret = new Secret(appName, {
    metadata: {
        namespace: namespace.metadata.name,
        name: appName,
        labels: commonLabels
    },
    type: "Opaque",
    stringData: {
        "ApiKey": config.requireSecret("ApiKey"),
        "PGSQL": config.requireSecret("PGSQL"),
    },
});


const labels: { [key: string]: string } = {
    "orleans/serviceId": appName,
    "orleans/clusterId": getStack(),
    ...commonLabels
};

const deployment = new Deployment(appName, {
    metadata: {
        name: appName,
        namespace: namespace.metadata.name,
        labels: labels
    },
    spec: {
        minReadySeconds: 60,
        replicas: config.getNumber(`replicas`) ?? 3,
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
                    name: "app",
                    image: config.require("image"),
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
                        initialDelaySeconds: 10
                    },
                    readinessProbe: {
                        httpGet: {
                            path: "/health",
                            port: 80
                        },
                        initialDelaySeconds: 10
                    },
                    env: [{
                        name: "ASPNETCORE_ENVIRONMENT",
                        value: getStack()
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
                        },
                    }, {
                        name: "OTEL_SERVICE_NAME",
                        valueFrom: {
                            fieldRef: {
                                apiVersion: "v1",
                                fieldPath: "metadata.labels['app.kubernetes.io/name']"
                            }
                        }
                    }, {
                        name: "OTEL_EXPORTER_OTLP_HOST",
                        valueFrom: {
                            fieldRef: {
                                apiVersion: "v1",
                                fieldPath: "status.hostIP"
                            }
                        }
                    }, {
                        name: "OTEL_EXPORTER_OTLP_ENDPOINT",
                        value: "http://$(OTEL_EXPORTER_OTLP_HOST):4317"
                    }],
                    envFrom: [{
                        secretRef: { name: secret.metadata.name }
                    }],
                }],
                terminationGracePeriodSeconds: 180
            }
        }
    }
});

const service = new Service(appName, {
    metadata: {
        name: appName,
        namespace: namespace.metadata.name,
        labels: commonLabels
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

var host = config.get("host");

if (host) {
    const ingress = new Ingress(appName, {
        metadata: {
            name: appName,
            namespace: namespace.metadata.name,
            labels: commonLabels,
            annotations: {
                "pulumi.com/skipAwait": "true",
                "cert-manager.io/cluster-issuer": "letsencrypt"
            }
        },
        spec: {
            ingressClassName: "nginx",
            tls: [
                {
                    hosts: [host],
                    secretName: `${appName}-tls-secret`
                }
            ],
            rules: [
                {
                    host: host,
                    http: {
                        paths: [{
                            path: "/",
                            pathType: "Prefix",
                            backend: {
                                // service: {
                                //     name: service.metadata.name,
                                //     port: { name: "http" }
                                // }
                                serviceName: service.metadata.name,
                                servicePort: "http"
                            }
                        }],
                    },
                }
            ]
        }
    });
}
