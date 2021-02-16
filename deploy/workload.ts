import * as k8s from "@pulumi/kubernetes";
import * as kx from "@pulumi/kubernetesx";
import { namespace, secret_dca_regcred } from "./core";

const image_version = process.env["IMAGE_VERSION"];
if (!image_version) { throw "missing IMAGE_VERSION" }
const pb = new kx.PodBuilder({
    imagePullSecrets: [secret_dca_regcred.metadata],
    containers: [{
        image: `ghcr.io/dcarea/fabron-service:${image_version}`,
        ports: { http: 80 },
    }],
});
export const deployment = new kx.Deployment("fabron-service", {
    metadata: {
        namespace: namespace.metadata.name
    },
    spec: pb.asDeploymentSpec({ replicas: 1 })
});

export const service = new k8s.core.v1.Service("fabron-service", {
    metadata: {
        namespace: namespace.metadata.name,
        annotations: { "external-dns.alpha.kubernetes.io/hostname": "fabron.doomed.app." }
    },
    spec: {
        ports: [{ name: "http", port: 80 }],
        selector: deployment.spec.template.metadata.labels,
        type: kx.types.ServiceType.LoadBalancer
    }

})
