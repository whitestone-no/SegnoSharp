##########
Kubernetes
##########

Namespace
=========

Because SegnoSharp comes as a Docker image, running it in Kubernetes (K8s) will of course also be possible.
The following has been tested in the K8s instance that comes with Docker Desktop.

First we need a namespace, so create a file called ``namespace.yml``:

::

    apiVersion: v1
    kind: Namespace
    metadata:
      name: segnosharp
      labels:
        name: segnosharp

Apply this file using the ``kubectl`` tool:

::

    kubectl apply -f namespace.yml

Then we need to activate the namespace so that everything we do will be connected to it:

::

    kubectl config set-context -current -namespace=segnosharp
	
.. note:: You can get a list of all available contexts using ``kubectl config get-contexts``

Volumes
=======

Now that we have the namespace and are working with it we need to define the storage volumes for the ``data`` and ``music`` folders.

First define the actual volumes in a file calles ``volumes.yml``:

::

    apiVersion: v1
    kind: PersistentVolume
    metadata:
      name: segnosharp-pv-data
      labels:
        type: local
    spec:
      storageClassName: segnosharp-data
      capacity:
        storage: 10Gi
      accessModes:
        - ReadWriteOnce
      hostPath:
        path: "/path/to/your/data"
    ---
    apiVersion: v1
    kind: PersistentVolume
    metadata:
      name: segnosharp-pv-music
      labels:
        type: local
    spec:
      storageClassName: segnosharp-music
      capacity:
        storage: 10Ti
      accessModes:
        - ReadWriteOnce
      hostPath:
        path: "/path/to/your/music"

Replace the ``path`` values with your actual paths. Apply this file as well:

::

    kubectl apply -f volumes.yml

Now we must create some volume claims. Create a new file called ``volumeclaims.yml`` with the following content:

::

    apiVersion: v1
    kind: PersistentVolumeClaim
    metadata:
      name: segnosharp-pvc-data
    spec:
      storageClassName: segnosharp-data
      accessModes:
        - ReadWriteOnce
      resources:
        requests:
          storage: 10Gi
    ---
    apiVersion: v1
    kind: PersistentVolumeClaim
    metadata:
      name: segnosharp-pvc-music
    spec:
      storageClassName: segnosharp-music
      accessModes:
        - ReadWriteOnce
      resources:
        requests:
          storage: 10Ti

And apply it:

::

    kubectl apply -f volumeclaims.yml

Deployment
==========

Finally you create the deployment of SegnoSharp itself. Create a new file called ``deployment.yml``:

::

    apiVersion: v1
    kind: Service
    metadata:
      name: segnosharp
    spec:
      ports:
      - port: 8080
      selector:
        app: segnosharp
      clusterIP: None
    ---
    apiVersion: apps/v1
    kind: Deployment
    metadata:
      name: segnosharp
    spec:
      selector:
        matchLabels:
          app: segnosharp
      strategy:
        type: Recreate
      template:
        metadata:
          labels:
            app: segnosharp
        spec:
          volumes:
          - name: segnosharp-storage-data
            persistentVolumeClaim:
              claimName: segnosharp-pvc-data
          - name: segnosharp-storage-music
            persistentVolumeClaim:
              claimName: segnosharp-pvc-music
          containers:
          - name: segnosharp
            image: ghcr.io/whitestone-no/segnosharp:latest
            imagePullPolicy: IfNotPresent
            ports:
            - containerPort: 8080
              name: http
            env:
            - name: SegnoSharp_OpenIdConnect__UseOidc
              value: "false"
            - name: SegnoSharp_CommonConfig__DataPath
              value: "/usr/segnosharp/data"
            - name: SegnoSharp_CommonConfig__MusicPath
              value: "/usr/segnosharp/music"
            volumeMounts:
              - mountPath: "/usr/segnosharp/data"
                name: segnosharp-storage-data
              - mountPath: "/usr/segnosharp/music"
                name: segnosharp-storage-music          

And apply this as well:

::

    kubectl apply -f deployment.yml

You should now have a SegnoSharp deployment in your K8s environment.

Finishing touches
=================

If you have nginx installed as an ingress engine in your K8s environment you can add SegnoSharp to it.
Create a new file, ``ingress.yml``, with the following content:

::

    apiVersion: networking.k8s.io/v1
    kind: Ingress
    metadata:
      name: segnosharp-ingress
    spec:
      ingressClassName: nginx
      rules:
      - host: segnosharp.local
        http:
          paths:
          - path: /
            pathType: Prefix
            backend:
              service:
                name: segnosharp
                port: 
                  number: 8080

Replace ``segnosharp.local`` with the hostname it should respond to.
Apply this file as well:

::

    kubectl apply -f deployment.yml

You should now be able to access SegnoSharp from the selected hostname.

Bonus
=====

Should you need to access a terminal inside the SegnoSharp deployment you can use the following command:

::

    kubectl exec -it <Identity of the SegnoSharp deployment/container> -- /bin/bash