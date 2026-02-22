import React from 'react';

interface ActionFooterProps {
    status: string;
}

const ActionFooter: React.FC<ActionFooterProps> = ({ status }) => {
    return (
        <div className="sidebar-footer">
            <div className="status-bar">{status}</div>
        </div>
    );
};

export default ActionFooter;
